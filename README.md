# MyLab.Log.Agent

[![Docker image](https://img.shields.io/static/v1?label=docker&style=flat&logo=docker&message=image&color=blue)](https://github.com/mylab-log/agent/pkgs/container/agent) [![License](https://img.shields.io/github/license/mylab-search-fx/delegate)](./LICENSE)

## Обзор

`MyLab.Log.Agent` собирает логи из файлов логов docker-контейнеров, разбирает их и  отправляет в `Elasticsearch`. 

При развёртывании необходимо указать директорию с логами и настройки подключения к `Elasticsearch`. Сервис будет отслеживать пополнение лого и обрабатывать и отправлять новые записи в `Elasticsearch`. 

## Обработка

### Алгоритм обработки

Алгоритм обработки логов:

* чтение очередной порции логов из файлов логов контейнеров
* попытка определить формат логов по метке контейнера `log_format`
* если формат определён, то:
  * происходит попытка собрать многострочный лог, при необходимости
  * происходит попытка достать ключевые поля
* осуществляется отправка в `Elasticsearch`.

Записи логов состоят из набора полей. Набор общих для всех полей:

* `log` - содержит всё лог-сообщение
* `time` - дата и время события
* `conatiner` - имя контейнера
* `level` - (опциональный) определяет уровень лога:
  * `error`
  * `warning`
  * `info`
  * `debug`
* `host_name` - имя сервера, устанавливается через переменную окружения `MLAGENT_HOST`
* `env` - имя контура, устанавливается через переменную окружения `MLAGENT_ENV`
* `message` - содержательная часть лог-сообщения. По умолчанию соответствует полю `log`
* `format` - определённый формат лога:
  * `net` - стандарьные консольные логи `.NETCore` и `.NET5+`
  * `mylab` - `yaml`-формат логов от [форматтера логов mylab](https://github.com/mylab-log/log#formatter-overview) 
  * `nginx` - `nginx`-логи в формате по умолчанию
  * `default` - не удаось определить.


### Формат `net`

Многострочный формат по умолчанию для стандартного консольного логгера `.NETCore` и `.NET5+`

Пример:

```
warn: Microsoft.AspNetCore.Server.Kestrel[22]
      Heartbeat took longer than "00:00:01" at "12/20/2021 07:02:26 +00:00". This could be caused by thread pool starvation.
```

Поле `level` определяется по ключевому слову в начале записи по принципу:

* `error` = `(fatal|fail|crit)`
* `warning` = `warn`
* `debug` = `dbug`
* `info` - по умолчанию и все другие значения

Поле `message` - вторая строка из лога. В примере выше это 

```
Heartbeat took longer than "00:00:01" at "12/20/2021 07:02:26 +00:00". This could be caused by thread pool starvation.
```

### Формат `mylab`

Многострочный `yaml`-формат логов от [форматтера логов mylab](https://github.com/mylab-log/log#formatter-overview). 

Пример:

```yaml
Message: Alarm!
Time: 2021-11-17T15:58:09.807
Labels:
  log-level: error
Facts:
  log-category: foo
```

Поле `level` определяется по факту `log-level`:

* `error` = `error`
* `warning` = `warning`
* `debug` = `debug`
* `info` - по умолчанию и все другие значения

Поле `message` - содержание узла `Message`.  Для примера выше это "**Alarm!**"

### Формат `nginx`

Однострочный формат `nginx` по умолчанию. Поддержка нестандартных форматов не гарантируется.

Пример:

```
2021/12/20 17:58:11 [warn] 30#30: *95 an upstream response is buffered to a temporary file /var/cache/nginx/proxy_temp/0/02/0000000020 while reading upstream, client: 192.168.80.169, server: *****.com, request: "GET /main.*******.js HTTP/1.0", upstream: "http://***.*.**.**:80/main.******.js", host: "****.com", referrer: "https://*****.com/authorization_code?code=*******&state=******"
```

Поле `level` определяется по наличию в строке лога соответствующего включения в квадратных скобках:

* `error` = `[(error|crit|alert|emerg)]`
* `warning` = `[warn]`
* `debug` = `[debug]`
* `info` - по умолчанию и все другие значения

Поле `message` для логов информационного уровня содержат http-метод, URL и код ответа. Например, для лога 

```
92.168.80.169 - - [13/Jan/2022:02:01:03 +0300] "GET /api/lk-state-provider/v1/state HTTP/1.0" 200 165 "https://dev-infonot.triasoft.com/" "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/97.0.4692.71 Safari/537.36" "62.217.190.115"
```

 поле `message` будет иметь следующие значение:

```
"GET /api/lk-state-provider/v1/state HTTP/1.0" 200
```

## Индексация

Для отправки записей в `Elasticsearch` формируется имя индекса на основании переменной из настроек в формате `logs-${MLAGENT_ES_INDEX}`. 

Таким образом будет задействовать встроенный шаблон индексов для логов.

## Развёртывание

### Подготовка Docker 

Для получения логов контейнеров и дополнительной служебной информации, необходимо дополнительно настроить `Docker` службу. Для этого необходимо в файле настроек службы `/etc/docker/daemon.json` указать следующие параметры:

```json
{
  "log-driver": "json-file",
  "log-opts":{
    "labels": "log_format",
    "tag": "{{.Name}}"
  }
}
```

Эти настройки позволят:

* собирать логи в файлы по умолчанию
* прикладывать к логам метку контейнера `log_format`
* прикладывать к логам имя контейнера

Для применения настроек необходимо перезапустить службу `Docker`:

```bash
systemctl restart docker
```

### Подготовка контейнеров

Разбор логов осуществляется с помощью механизмов, специфических для каждого поддерживаемого формата. Формат логов приложения можно указать в виде метки контейнера `log_format`. 

Поддерживаются следующие значения:

* `net` - стандарьные консольные логи `.NETCore` и `.NET5+`
* `mylab` - `yaml`-формат логов от [форматтера логов mylab](https://github.com/mylab-log/log#formatter-overview) 
* `nginx` - `nginx`-логи в формате по умолчанию.

Остальные значения, в том числе и без метки, интерпретируются, как неформатированные однострочные логи.

### MyLab.Log.Agent контейнер

Сервис подготовлен к развёртыванию в `docker`-контейнере.

При этом необходимо:

* подключить директорию с лог-файлами контейнеров в `/var/lib/mylab-logagent/src/containers`
* подключить или оставить по умолчанию директорию данных сервиса `/var/lib/mylab-logagent/data`
* определить параметры взаимодействия с `Elasticsearch` через переменные окружения:
  * `MLAGENT_ES_HOST` - IP или хост, `127.0.0.1` - по умолчанию
  * `MLAGENT_ES_PORT` - tcp порт, `9200` - по умолчанию
  * `MLAGENT_ES_PATH` - относительный путь http запроса, пустая строка по умолчанию
  * `MLAGENT_ES_INDEX` - префикс имени индекса, `mlagent` - по умолчанию
* определить параметры окружения через переменные окружения:
  * `MLAGENT_HOST` - имя хостовой машины, `undefined` - по умолчанию
  * `MLAGENT_ENV` - имя контура, `undefined` - по умолчанию
* определить настройки сервиса через переменные окружения:
  * `MLAGENT_LOGLEVEL` - уровень логирования ([подробнее про log_level](https://docs.fluentbit.io/manual/administration/configuring-fluent-bit/configuration-file)), возможные значения:
    * `off`
    * `error`
    * `warn`
    * `nfo` (по умолчанию)
    * `debug`
    * `trace`

Пример:

```yaml
version: '3.2'

services: 
    log-agent:
        container_name: log-agent
        image: ghcr.io/mylab-log/agent:latest
        environment:
          MLAGENT_ES_HOST: 192.168.80.198
          MLAGENT_ES_INDEX: my-app
          MLAGENT_HOST: dev-app
          MLAGENT_ENV: dev
          MLAGENT_LOGLEVEL: debug
        volumes:
          - /etc/localtime:/etc/localtime:ro
          - /var/lib/docker/containers:/var/lib/mylab-logagent/src/containers
          - log_agent_data:/var/lib/mylab-logagent/data
      
volumes:
  log_agent_data: {}
```

