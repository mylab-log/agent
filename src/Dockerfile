ARG FLUENT_BIT_VER=[undefined]

FROM fluent/fluent-bit:${FLUENT_BIT_VER}

COPY ./lua-filter/yaml-filter.lua /usr/lib/mylab-logagent/filters/yaml-filter.lua
COPY ./lua-filter/json.lua /usr/local/share/lua/5.1/json.lua

ENV MLAGENT_ES_HOST=127.0.0.1
ENV MLAGENT_ES_PORT=9200
ENV MLAGENT_ES_PATH=
ENV MLAGENT_ES_INDEX=mlagent
ENV MLAGENT_HOST=undefined
ENV MLAGENT_ENV=undefined
ENV MLAGENT_LOGLEVEL=info

#install system packages
RUN apt-get update -y  &&\
    apt-get install -y wget unzip build-essential libreadline-dev

WORKDIR /tmp/install

#Install lua
RUN wget http://www.lua.org/ftp/lua-5.1.5.tar.gz  &&\
    tar zxf lua-5.1.5.tar.gz
WORKDIR lua-5.1.5
RUN make linux  &&\
    make install

WORKDIR ..

#Install luarocks
RUN wget http://luarocks.github.io/luarocks/releases/luarocks-3.9.2.tar.gz &&\
    tar zxf luarocks-3.9.2.tar.gz
WORKDIR luarocks-3.9.2
RUN ./configure --with-lua-include=/usr/local/include &&\
    make &&\
    make install

#Cleanup after installation 
RUN rm -rf /tmp/install

#Install lua packages
RUN luarocks --server=http://rocks.moonscript.org install lyaml

ADD ./fluent-bit-config/config.conf /fluent-bit/etc/fluent-bit.conf
ADD ./fluent-bit-config/includes /fluent-bit/etc/includes
ADD ./fluent-bit-config/parsers.conf /fluent-bit/etc/parsers.conf

VOLUME ["/var/lib/mylab-logagent/data"]