local lyaml = require "lyaml"

function extract_all(tag, timestamp, record)

    local log_rec = record["log"]

    if log_rec == nil then 
        return 0, timestamp, record
    end

    local doc = lyaml.load(log_rec)
    
    if doc == nil then 
        return 0, timestamp, record
    end

    extract_facts(doc["Facts"], record)
    extract_exception(doc["Exception"], record)
    
    local labels = doc["Labels"]
    extract_loglevel(labels, record)
    extract_labels(labels, record)

    return 2, timestamp, record
end

local funcion extract_facts(doc, record)

    local facts = doc["Facts"]
    if facts ~= nil then
        local facts_str = lyaml.dump(facts)
        record["facts"] = facts_str
    end

end

local funcion extract_exception(doc, record)

    local ex = doc["Exception"]
    if ex ~= nil then
        local ex_str = lyaml.dump(ex)
        record["exception"] = ex_str
    end

end

local funcion extract_lavel(labels, record)

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

local funcion extract_labels(labels, record)

    for n,v in ipairs(labels) do
        if n ~= "log_level" and n ~= "log-level" then
            record[n] = v
        end
    end

end