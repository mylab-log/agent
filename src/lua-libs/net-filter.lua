local function extract_net_log(tag, timestamp, record)
{
    local log_rec = record["log"]

    if log_rec == nil then 
        return 0, timestamp, record
    end

    string.sub(log_rec, 20)

    return 2, timestamp, record
}