package main

import (
	"fmt"
	"os"
	"strings"
	"unsafe"

	"github.com/valyala/fastjson"
	"gopkg.in/yaml.v3"
)

//export go_filter
func go_filter(tag *uint8, tag_len uint, time_sec uint, time_nsec uint, record *uint8, record_len uint) *uint8 {

	str := go_filter_logic(tag, tag_len, time_sec, time_nsec, record, record_len)

	if str == nil {
		return nil
	}

	rv := []byte(*str)

	return &rv[0]
}

func test_filter(content string) {
	fileContent := []byte(content)
	tag := []byte("test-tag")
	result := go_filter_logic(&tag[0], uint(len(tag)), 0, 0, &fileContent[0], uint(len(fileContent)))
	fmt.Println(*result)
}

func main() {

	if len(os.Args) == 2 {

		test_filter(strings.ReplaceAll(os.Args[1], "[quotes]", "\""))
	}
}

func go_filter_logic(tag *uint8, tag_len uint, time_sec uint, time_nsec uint, record *uint8, record_len uint) *string {

	defer func() {
		if err := recover(); err != nil {
			fmt.Println("panic occurred:", err)
		}
	}()

	brecord := unsafe.Slice(record, record_len)

	br := string(brecord)

	var p fastjson.Parser
	value, err := p.Parse(br)
	if err != nil {
		return nil
	}

	obj, err := value.Object()
	if err != nil {
		return nil
	}

	ymlValue := obj.Get("log")

	ymlBin, err := ymlValue.StringBytes()
	if err != nil {
		fmt.Println("[log-tostr] - ", err)
		return nil
	}

	ymlMap := make(map[interface{}]interface{})
	err = yaml.Unmarshal(ymlBin, ymlMap)
	if err != nil {
		fmt.Println("[unmarsh-bin] - ", err)
		return nil
	}

	labels := ymlMap["Labels"].(map[string]interface{})

	extract_message(obj, ymlMap)
	extract_level(obj, labels)
	extract_another_labels(obj, labels)
	extract_exception(obj, ymlMap)
	extract_facts(obj, ymlMap)

	s := obj.String()
	s += string(rune(0)) // Note: explicit null terminator.

	return &s
}

func extract_message(obj *fastjson.Object, ymlMap map[interface{}]interface{}) {

	var arena fastjson.Arena
	obj.Set("message", arena.NewString(ymlMap["Message"].(string)))

}

func extract_facts(obj *fastjson.Object, ymlMap map[interface{}]interface{}) {

	facts := ymlMap["Facts"]

	if facts != nil {

		factsBin, err := yaml.Marshal(&facts)
		if err != nil {
			fmt.Println(err)
		}
		var arena fastjson.Arena
		obj.Set("facts", arena.NewString(string(factsBin)))
	}

}

func extract_exception(obj *fastjson.Object, ymlMap map[interface{}]interface{}) {

	exception := ymlMap["Exception"]

	if exception != nil {

		exceptionBin, err := yaml.Marshal(&exception)
		if err != nil {
			fmt.Println(err)
		}

		var arena fastjson.Arena
		obj.Set("exception", arena.NewString(string(exceptionBin)))
	}

}

func extract_level(obj *fastjson.Object, labels map[string]interface{}) {

	logLevel1 := labels["log-level"]
	logLevel2 := labels["log_level"]

	logLevel := "info"

	if logLevel1 != nil {
		logLevel = logLevel1.(string)
	} else if logLevel2 != nil {
		logLevel = logLevel2.(string)
	}

	var arena fastjson.Arena
	obj.Set("level", arena.NewString(logLevel))

}

func extract_another_labels(obj *fastjson.Object, labels map[string]interface{}) {

	for key, element := range labels {

		if key != "log-level" && key != "log_level" && obj.Get(key) == nil {

			var arena fastjson.Arena
			obj.Set(key, arena.NewString(element.(string)))

		}
	}

}
