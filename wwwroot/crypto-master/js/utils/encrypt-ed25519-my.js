(function(myEncryptUtils) {
'use strict';
/*
 * import the dependencies for ed25519 and base58
 * use: need them before this!
 * */

/**
 * generate the byte[] KeyPair use ed25519
 */
myEncryptUtils.generateKeyPairByte = function(){
	var keyPair = nacl.sign.keyPair();
	var pk = keyPair.publicKey;
	var sk = keyPair.secretKey.slice(0,32);
	return {publicKey: pk, privateKey: sk};
};

myEncryptUtils.Seed = function () {
    var sk = new Uint8Array(32);
    randombytes(sk, 32);
    return sk;
};
/**
 * generate the base58 encode KeyPair use ed25519
 */
myEncryptUtils.generateKeyPair = function (seed) {
    var keyPair = nacl.sign.keyPair.fromSeed(seed);
    var pk = keyPair.publicKey;
    var sk = keyPair.secretKey.slice(0, 32);
    return { publicKey: pk, privateKey: sk };
};

/**
 * get the publickey from privateKey
 * @param {Object} privateKey
 */
myEncryptUtils.getPublicKeyByPrivateKey = function(privateKey){
    var secretKey = myEncryptUtils.stringToBytes(privateKey)
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
myEncryptUtils.sign = function(privateKey, msg){
	/*----------- convert the privateKey(base58 32) to secretKey(Uint8Array 64) ----------*/
    var publicKey = myEncryptUtils.getPublicKeyByPrivateKey(privateKey);
    var secretKeyByte = myEncryptUtils.stringToBytes(privateKey);
    var publicKeyByte = myEncryptUtils.stringToBytes(publicKey);
	var secretKeyFull = new Uint8Array(64);
	secretKeyByte = publicKeyByte.reduce( function(coll,item){  
	    coll.push( item );  
	    return coll;  
	}, secretKeyByte ); 
	secretKeyFull.set(secretKeyByte)
	/*----------- convert the msg(string) to msg(Uint8Array ) ---------*/
    var msgByte = myEncryptUtils.stringToBytes(msg);
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
myEncryptUtils.verify = function(msg, sig, publicKey){
    var msgByte = myEncryptUtils.stringToBytes(msg);
	var sigByte = sig;
    var publicKeyByte = myEncryptUtils.stringToBytes(publicKey);
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
myEncryptUtils.stringToBytes = function(str){  
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
})(typeof module !== 'undefined' && module.exports ? module.exports : (self.myEncryptUtils = self.myEncryptUtils || {}));