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
 * generate the base58 encode KeyPair use ed25519
 */
EncryptUtils.generateKeyPair = function(){
	var keyPair = nacl.sign.keyPair();
	var pk = Base58.encode(keyPair.publicKey);
	var sk = Base58.encode(keyPair.secretKey.slice(0,32));
	return {publicKey: pk, privateKey: sk};
};


/**
 * get the publickey from privateKey
 * @param {Object} privateKey
 */
EncryptUtils.getPublicKeyByPrivateKey = function(privateKey){
	var secretKey = Base58.decodeArray(privateKey)
	var secretKeyUnit8Array = new Uint8Array(32);
	secretKeyUnit8Array.set(secretKey)
	var keyPair = nacl.sign.keyPair.fromSeed(secretKeyUnit8Array);
	var publickKeyBase58 = Base58.encode(keyPair.publicKey);
	return publickKeyBase58;
};

/**
 * sign the msg with privateKey
 * @param {Object} msg
 * @param {Object} secretKey
 */
EncryptUtils.sign = function(privateKey, msg){
	/*----------- convert the privateKey(base58 32) to secretKey(Uint8Array 64) ----------*/
	var publicKey = EncryptUtils.getPublicKeyByPrivateKey(privateKey);
	var secretKeyByte = Base58.decodeArray(privateKey);
	var publicKeyByte = Base58.decodeArray(publicKey);
	var secretKeyFull = new Uint8Array(64);
	secretKeyByte = publicKeyByte.reduce( function(coll,item){  
	    coll.push( item );  
	    return coll;  
	}, secretKeyByte ); 
	secretKeyFull.set(secretKeyByte)
	/*----------- convert the msg(string) to msg(Uint8Array ) ---------*/
	var msgByte = EncryptUtils.stringToBytes(msg);
	var msgUnit8Array = new Uint8Array(msgByte.length);
	msgUnit8Array.set(msgByte);
	var signedMsg = nacl.sign.detached(msgUnit8Array, secretKeyFull);
	return Base58.encode(signedMsg);
};

/**
 * sig msg verify
 * @param {Object} msg
 * @param {Object} sig
 * @param {Object} publicKey
 */
EncryptUtils.verify = function(msg, sig, publicKey){
	var msgByte = EncryptUtils.stringToBytes(msg);
	var sigByte = Base58.decodeArray(sig);
	var publicKeyByte = Base58.decodeArray(publicKey);
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