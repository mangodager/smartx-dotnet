(function(JsonUtils) {
'use strict';

/**
 * empty obj 
 * @param {Object} e
 */
function isEmptyObject(e) {  
    var t;  
    for (t in e)  
        return !1;  
    return !0  
} 

/**
 * {},[] will replace with null
 * sort json obj by all keys
 * @param {Object} jsonObj
 */
var sortKeys = function(jsonObj) {
	var ordered = {};
	if(jsonObj != undefined && jsonObj != null){
		if(jsonObj instanceof Array){
			if(jsonObj.length == 0){
				ordered = null;
			}else{
				var arrary = [jsonObj.length];
				for (var i=0 ; i< jsonObj.length; i++) {
					if(typeof(jsonObj[i]) == "string" || typeof(jsonObj[i]) == "number" || typeof(jsonObj[i]) == "boolean"){
						arrary[i] = jsonObj[i]
					}else if(typeof(jsonObj[i]) == "object"){
						arrary[i] = sortKeys(jsonObj[i]);
					}else{
						//todo 
						arrary[i] = jsonObj[i];
					}
				}
				ordered = arrary;
			}
		}else if(typeof(jsonObj) == "object") {
			if(isEmptyObject(jsonObj)){
				ordered = null;
			}else{
				Object.keys(jsonObj).sort().forEach(function(key){
					ordered[key] = sortKeys(jsonObj[key]);
				});
			}
		}else{
			ordered = jsonObj;
		}
	}else{
		return null;
	}
	return ordered;
};

/**
 * sort json obj by the outer keys, depth is 1.
 * @param {Object} jsonObj
 */
var sortBaseKeys = function(jsonObj) {
	var ordered = {};
	Object.keys(jsonObj).sort().forEach(function(key) {
		ordered[key] = jsonObj[key];
	});
	return ordered;
};

/**
 * parse the string of json to json obj
 * @param {Object} jsonStr
 */
JsonUtils.parse = function(jsonStr){
	if(typeof(jsonStr) == "string"){
		try{
			return JSON.parse(jsonStr);
		}catch(e){
			throw new Error("json parse error");	
		}
	}else{
		throw new TypeError("parameter must be string of json");	
	}
}

/**
 * stringify the input, the json obj is better
 * @param {Object} jsonObj
 */
JsonUtils.stringify = function(jsonObj){
	return JSON.stringify(jsonObj);
}

/**
 * sort json obj by all keys
 * @param {Object} jsonObj
 */
JsonUtils.sortKeys = function(jsonObj){
	var jsonStr = JsonUtils.stringify(jsonObj);
	jsonObj = JsonUtils.parse(jsonStr);
	return sortKeys(jsonObj);
};

/**
 * sort json obj by the outer keys, depth is 1.
 * @param {Object} jsonObj
 */
JsonUtils.softByBaseKey = function(jsonObj){
	var jsonStr = JsonUtils.stringify(jsonObj);
	jsonObj = JsonUtils.parse(jsonStr);
	return sortBaseKeys(jsonObj);
};

/**
 * string to byte
 * @param {Object} str
 */
JsonUtils.stringToBytes = function(str){  
  	var ch, st, re = [];  
  	for (var i = 0; i < str.length; i++ ) {  
    	ch = str.charCodeAt(i);  // get char   
    	st = [];                 // set up "stack"  
    	do {  
      		st.push( ch & 0xFF );  // push byte to stack  
      		ch = ch >> 8;          // shift value down by 1 byte  
    	}    
    	while(ch);  
		// add stack contents to result  
		// done because chars have "wrong" endianness  
		re = re.concat( st.reverse() );  
	}  
	// return an array of bytes  
	return re;  
};
})(typeof module !== 'undefined' && module.exports ? module.exports : (self.JsonUtils = self.JsonUtils || {}));