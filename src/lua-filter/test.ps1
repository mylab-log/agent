$curPath = Resolve-Path .

docker run `
    -v $curPath\yaml-filter.lua:/test/yaml-filter.lua:ro `
    -v $curPath\test.log:/test/test.log:ro `
    -v $curPath\test.conf:/fluent-bit/etc/fluent-bit.conf:ro `
    --rm `
    -ti `
    fluent/fluent-bit:2.1.8-debug