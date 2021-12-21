FROM fluent/fluent-bit:1.8.8

ADD ./src/config.conf /fluent-bit/etc/fluent-bit.conf
ADD ./src/parsers.conf /fluent-bit/etc/parsers.conf

VOLUME ["/var/lib/mylab-logagent/data"]