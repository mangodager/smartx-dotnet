(function(EncryptUtils) {
'use strict';
/*
 * import the dependencies for ed25519 and base58
 * use: need them before this!
 * */

/**
 * generate the byte[] KeyPair use ed25519
 */
EncryptUtils.generateKeyPairByte = function(){
	var keyPair = nacl.sign.keyPair();
	var pk = keyPair.publicKey;
	var sk = keyPair.secretKey.slice(0,32);
	return {publicKey: pk, privateKey: sk};
};

/**
 * generate generateSeed
 */
EncryptUtils.generateSeed = function(){
    var numArr = new Uint8Array(32);
    for (var i = 0; i < numArr.length; i++)
    {
        numArr[i] = Math.random() * 255;
    }
    return numArr;
};

/**
 * generate the base58 encode KeyPair use ed25519
 */
EncryptUtils.generateKeyPair = function(){
	var keyPair = nacl.sign.keyPair();
	var pk = Base58.encode(keyPair.publicKey);
	var sk = Base58.encode(keyPair.secretKey.slice(0,32));
	return {publicKey: pk, privateKey: sk};
};

EncryptUtils.Bytes2Hex = function (arr) {
    var str = "";
    for (var i = 0; i < arr.length; i++) {
        var tmp = arr[i].toString(16);
        if (tmp.length == 1) {
            tmp = "0" + tmp;
        }
        str += tmp;
    }
    return str;
};

/**
    * generate the base58 encode KeyPair use ed25519
    */
EncryptUtils.generateKeyPairSeed = function (seed) {
    var keyPair = nacl.sign.keyPair.fromSeed(seed);
    var pk = keyPair.publicKey;
    var sk = keyPair.secretKey;
    return { publicKey: pk, privateKey: sk };
};

/**
 * get the publickey from privateKey
 * @param {Object} privateKey
 */
EncryptUtils.getPublicKeyByPrivateKey = function(privateKey){
    var secretKey = privateKey.slice(0, 32)
	var secretKeyUnit8Array = new Uint8Array(32);
	secretKeyUnit8Array.set(secretKey)
	var keyPair = nacl.sign.keyPair.fromSeed(secretKeyUnit8Array);
    return keyPair.publicKey;
};

/**
 * sign the msg with privateKey
 * @param {Object} msg
 * @param {Object} secretKey
 */
EncryptUtils.sign = function(privateKey, msg){
	/*----------- convert the privateKey(base58 32) to secretKey(Uint8Array 64) ----------*/
    var secretKeyFull = privateKey;
	/*----------- convert the msg(string) to msg(Uint8Array ) ---------*/
    var msgByte = msg;
	var msgUnit8Array = new Uint8Array(msgByte.length);
	msgUnit8Array.set(msgByte);
	var signedMsg = nacl.sign.detached(msgUnit8Array, secretKeyFull);
	return signedMsg;
};

/**
 * sig msg verify
 * @param {Object} msg
 * @param {Object} sig
 * @param {Object} publicKey
 */
EncryptUtils.verify = function(msg, sig, publicKey){
    var msgByte = msg;
	var sigByte = sig;
	var publicKeyByte = publicKey;
	var publicKeyUnit8Array = new Uint8Array(publicKeyByte.length);
	var sigByteUnit8Array = new Uint8Array(sigByte.length);
	var msgByteUnit8Array = new Uint8Array(msgByte.length);

	publicKeyUnit8Array.set(publicKeyByte);
	sigByteUnit8Array.set(sigByte);
	msgByteUnit8Array.set(msgByte);

	return nacl.sign.detached.verify(msgByteUnit8Array, sigByteUnit8Array, publicKeyUnit8Array);
};

/**
 * string to byte
 * @param {Object} str
 */
EncryptUtils.stringToBytes = function(str){  
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


})(typeof module !== 'undefined' && module.exports ? module.exports : (self.EncryptUtils = self.EncryptUtils || {}));