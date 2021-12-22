FROM fluent/fluent-bit:1.8.8

ENV MLAGENT_ES_HOST=127.0.0.1
ENV MLAGENT_ES_PORT=9200
ENV MLAGENT_ES_PATH=
ENV MLAGENT_ES_INDEX=logs

ADD ./src/config.conf /fluent-bit/etc/fluent-bit.conf
ADD ./src/includes /fluent-bit/etc/includes
ADD ./src/parsers.conf /fluent-bit/etc/parsers.conf

VOLUME ["/var/lib/mylab-logagent/data"]