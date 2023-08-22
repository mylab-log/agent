$curPath = Resolve-Path .

docker run `
    -v $curPath\test.log:/test/test.log `
    -v $curPath\input.conf:/fluent-bit/etc/includes/input.conf:ro `
    -v $curPath\output.conf:/fluent-bit/etc/includes/output.conf:ro `
    -v $curPath\common-final.conf:/fluent-bit/etc/includes/common-final.conf:ro `
    -e MLAGENT_HOST=test `
    -e MLAGENT_ENV=test `
    --rm `
    -ti `
    mylab-log/agent:debug