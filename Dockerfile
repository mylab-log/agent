FROM fluent/fluent-bit:1.8.8

ADD ./config.yml:/fluent-bit/etc/fluent-bit.conf
ADD ./parsers.conf:/fluent-bit/etc/parsers.conf

VOLUME ["/var/lib/mylab-logagent/data"]