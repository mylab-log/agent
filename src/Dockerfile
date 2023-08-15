FROM tinygo/tinygo:0.28.1 AS build
WORKDIR /home/tinygo/src
COPY ./yml-filter .
RUN mkdir /home/tinygo/output
RUN tinygo build -target=wasi -o /home/tinygo/output/yml-filter.wasm filter.go

# final stage/image
FROM fluent/fluent-bit:2.0.8

WORKDIR /usr/lib/fluent-bit/filters
COPY --from=build /home/tinygo/output /usr/lib/fluent-bit/filters

ENV MLAGENT_ES_HOST=127.0.0.1
ENV MLAGENT_ES_PORT=9200
ENV MLAGENT_ES_PATH=
ENV MLAGENT_ES_INDEX=mlagent
ENV MLAGENT_HOST=undefined
ENV MLAGENT_ENV=undefined
ENV MLAGENT_LOGLEVEL=info

ADD ./fluent-bit-config/config.conf /fluent-bit/etc/fluent-bit.conf
ADD ./fluent-bit-config/input.conf /fluent-bit/etc/input.conf
ADD ./fluent-bit-config/includes /fluent-bit/etc/includes
ADD ./fluent-bit-config/parsers.conf /fluent-bit/etc/parsers.conf

VOLUME ["/var/lib/mylab-logagent/data"]