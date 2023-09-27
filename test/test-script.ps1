$curPath = Resolve-Path .
$srcPath = Resolve-Path ..\src

docker run `
    -v $curPath\$($args[0]).log:/test/test.log `
    -v $curPath\input.conf:/fluent-bit/etc/includes/input.conf:ro `
    -v $curPath\output.conf:/fluent-bit/etc/includes/output.conf:ro `
    -v $srcPath\lua-libs\yaml-filter.lua:/usr/lib/mylab-logagent/filters/yaml-filter.lua:ro `
    -v $srcPath\lua-libs\net-filter.lua:/usr/lib/mylab-logagent/filters/net-filter.lua:ro `
    -v $srcPath\includes\net.conf:/fluent-bit/etc/includes/net.conf:ro `
    -e MLAGENT_HOST=test `
    -e MLAGENT_ENV=test `
    --rm `
    -ti `
    mylab-log/agent:debug