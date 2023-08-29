$curPath = Resolve-Path .

docker run `
    -v $curPath\$($args[0]).log:/test/test.log `
    -v $curPath\input.conf:/fluent-bit/etc/includes/input.conf:ro `
    -v $curPath\output.conf:/fluent-bit/etc/includes/output.conf:ro `
    -e MLAGENT_HOST=test `
    -e MLAGENT_ENV=test `
    --rm `
    -ti `
    mylab-log/agent:debug