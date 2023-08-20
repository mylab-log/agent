$rawContent = Get-Content .\single.log
$content = $rawContent.Replace("`"","[quotes]")


 wasmer run ./mylab-yml-filter.wasm $content
# wavm run ./mylab-yml-filter.wasm $file
# wasmtime --dir=$curDir --dir=. ./mylab-yml-filter.wasm $file