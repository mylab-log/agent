$srcPath = Resolve-Path ..\src
$curPath = Resolve-Path .

docker run `
    -v $srcPath\fluent-bit-config\config.conf:/fluent-bit/etc/fluent-bit.conf:ro `
    -v $srcPath\fluent-bit-config\includes:/fluent-bit/etc/includes `
    -v $srcPath\fluent-bit-config\parsers.conf:/fluent-bit/etc/parsers.conf:ro `
    -v $curPath\test-logs:/var/lib/mylab-logagent/src/test-logs `
    -v $curPath\input.conf:/fluent-bit/etc/includes/input.conf:ro `
    -v $curPath\output.conf:/fluent-bit/etc/includes/output.conf:ro `
    -v $curPath\common-final.conf:/fluent-bit/etc/includes/common-final.conf:ro `
    -v $srcPath\yml-filter\mylab-yml-filter.wasm:/usr/lib/fluent-bit/filters/mylab-yml-filter.wasm:ro `
    -e MLAGENT_HOST=test `
    -e MLAGENT_ENV=test `
    --rm `
    -ti `
    fluent/fluent-bit:2.1.8-debug