local lyaml = require "lyaml"
local json = require "json"

local function cleanup_yml(yml)

    if string.len(yml) < 11 then
        return yml
    end
    
    return string.sub(yml, 5, -6)

end

local function extract_message(doc, record)

    local msg = doc["Message"]
    if msg ~= nil then
        record["message"] = msg
    end

end

local function extract_facts(doc, record)

    local facts = doc["Facts"]
    if facts ~= nil then
        
        local facts_str = lyaml.dump( {facts} )
        local res = cleanup_yml(facts_str)
        record["facts"] = res
    end

end 

local function extract_exception(doc, record)

    local ex = doc["Exception"]
    if ex ~= nil then
        local ex_str = lyaml.dump( {ex} )
        record["exception"] = cleanup_yml(ex_str)
    end

end

local function extract_level(labels, record)

    local lvl1 = labels["log_level"]
    local lvl2 = labels["log-level"]
    local lvl = lvl1

    if lvl == nil then
        lvl = lvl2
    end

    if lvl ~= nil then
        record["level"] = lvl
    else
        record["level"] = "info"
    end

end

local function extract_labels(labels, record)

    for k,v in pairs(labels) do
        if k ~= "log_level" and k ~= "log-level" then
            record[k] = v
        end
    end

end

local function extract_all_core(tag, timestamp, record)
    
    local log_rec = record["log"]

    if log_rec == nil then 
        return 0, timestamp, record
    end

    local ok, doc_or_error = pcall(lyaml.load, log_rec)
    
    if not ok then
        error({msg="'log' parsing error", inner=doc_or_error, log_origin=log_rec})
    end

    local doc = doc_or_error
    if doc == nil then 
        return 0, timestamp, record
    end

    extract_message(doc, record)
    extract_facts(doc, record)
    extract_exception(doc, record)
    
    local labels = doc["Labels"]
    if labels ~= nil then
        extract_level(labels, record)
        extract_labels(labels, record)
    end

    return 2, timestamp, record
end

function extract_all(tag, timestamp, record)
    
    local ok, result_tag_or_error, result_timestamp,result_record = pcall(extract_all_core,tag, timestamp, record)

    if not ok then
        local raw_error_str = lyaml.dump( {result_tag_or_error} )
        record["log-agent:parsing-error"] = cleanup_yml(raw_error_str)
        record["message"] = '[log parsing error]'
        return 2, timestamp, record
    end

    return result_tag_or_error, result_timestamp,result_record
end