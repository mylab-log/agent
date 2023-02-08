FROM fluent/fluent-bit:1.8.8

ENV MLAGENT_ES_HOST=127.0.0.1
ENV MLAGENT_ES_PORT=9200
ENV MLAGENT_ES_PATH=
ENV MLAGENT_ES_INDEX=mlagent
ENV MLAGENT_HOST=undefined
ENV MLAGENT_ENV=undefined
ENV MLAGENT_LOGLEVEL=info

ADD ./src/config.conf /fluent-bit/etc/fluent-bit.conf
ADD ./src/input.conf /fluent-bit/etc/input.conf
ADD ./src/includes /fluent-bit/etc/includes
ADD ./src/parsers.conf /fluent-bit/etc/parsers.conf

VOLUME ["/var/lib/mylab-logagent/data"]