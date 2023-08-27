local lyaml = require "lyaml"
local json = require "json"

local function extract_message(doc, record)

    local msg = doc["Message"]
    if msg ~= nil then
        record["message"] = msg
    end

end

local function extract_facts(doc, record)

    local facts = doc["Facts"]
    if facts ~= nil then
        
        factsArr = { facts }
        
        print("Facts >>>")
        -- print(json.encode(facts))
        print(lyaml.dump(factsArr))
        print("Facts <<<")
        local facts_str = lyaml.dump(facts)
        record["facts"] = facts_str
    end

end 

local function extract_exception(doc, record)

    local ex = doc["Exception"]
    if ex ~= nil then
        local ex_str = lyaml.dump(ex)
        record["exception"] = ex_str
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

    for n,v in ipairs(labels) do
        if n ~= "log_level" and n ~= "log-level" then
            record[n] = v
        end
    end

end

function extract_all(tag, timestamp, record)
    
    local log_rec = record["log"]

    if log_rec == nil then 
        return 0, timestamp, record
    end

    local doc = lyaml.load(log_rec)
    
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