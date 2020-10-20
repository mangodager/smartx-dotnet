(function(EncryptUtils) {
'use strict';

var jsSHA = require("jssha");
var moment = require('moment');
var Base58 = require('bs58');
// var utf8 = require('utf8');
var nacl=require('tweetnacl')
/*
 * import the dependencies for ed25519 and base58
 * use: need them before this!
 * */


 /**
  * Make sure the charset of the page using this script is
  * set to utf-8 or you will not get the correct results.
  */
 var utf8 = (function () {
     var highSurrogateMin = 0xd800,
         highSurrogateMax = 0xdbff,
         lowSurrogateMin  = 0xdc00,
         lowSurrogateMax  = 0xdfff,
         surrogateBase    = 0x10000;

     function isHighSurrogate(charCode) {
         return highSurrogateMin <= charCode && charCode <= highSurrogateMax;
     }

     function isLowSurrogate(charCode) {
         return lowSurrogateMin <= charCode && charCode <= lowSurrogateMax;
     }

     function combineSurrogate(high, low) {
         return ((high - highSurrogateMin) << 10) + (low - lowSurrogateMin) + surrogateBase;
     }

     /**
      * Convert charCode to JavaScript String
      * handling UTF16 surrogate pair
      */
     function chr(charCode) {
         var high, low;

         if (charCode < surrogateBase) {
             return String.fromCharCode(charCode);
         }

         // convert to UTF16 surrogate pair
         high = ((charCode - surrogateBase) >> 10) + highSurrogateMin,
         low  = (charCode & 0x3ff) + lowSurrogateMin;

         return String.fromCharCode(high, low);
     }

     /**
      * Convert JavaScript String to an Array of
      * UTF8 bytes
      * @export
      */
     function stringToBytes(str) {
         var bytes = [],
             strLength = str.length,
             strIndex = 0,
             charCode, charCode2;

         while (strIndex < strLength) {
             charCode = str.charCodeAt(strIndex++);

             // handle surrogate pair
             if (isHighSurrogate(charCode)) {
                 if (strIndex === strLength) {
                     throw new Error('Invalid format');
                 }

                 charCode2 = str.charCodeAt(strIndex++);

                 if (!isLowSurrogate(charCode2)) {
                     throw new Error('Invalid format');
                 }

                 charCode = combineSurrogate(charCode, charCode2);
             }

             // convert charCode to UTF8 bytes
             if (charCode < 0x80) {
                 // one byte
                 bytes.push(charCode);
             }
             else if (charCode < 0x800) {
                 // two bytes
                 bytes.push(0xc0 | (charCode >> 6));
                 bytes.push(0x80 | (charCode & 0x3f));
             }
             else if (charCode < 0x10000) {
                 // three bytes
                 bytes.push(0xe0 | (charCode >> 12));
                 bytes.push(0x80 | ((charCode >> 6) & 0x3f));
                 bytes.push(0x80 | (charCode & 0x3f));
             }
             else {
                 // four bytes
                 bytes.push(0xf0 | (charCode >> 18));
                 bytes.push(0x80 | ((charCode >> 12) & 0x3f));
                 bytes.push(0x80 | ((charCode >> 6) & 0x3f));
                 bytes.push(0x80 | (charCode & 0x3f));
             }
         }

         return bytes;
     }

     /**
      * Convert an Array of UTF8 bytes to
      * a JavaScript String
      * @export
      */
     function bytesToString(bytes) {
         var str = '',
             length = bytes.length,
             index = 0,
             byte,
             charCode;

         while (index < length) {
             // first byte
             byte = bytes[index++];

             if (byte < 0x80) {
                 // one byte
                 charCode = byte;
             }
             else if ((byte >> 5) === 0x06) {
                 // two bytes
                 charCode = ((byte & 0x1f) << 6) | (bytes[index++] & 0x3f);
             }
             else if ((byte >> 4) === 0x0e) {
                 // three bytes
                 charCode = ((byte & 0x0f) << 12) | ((bytes[index++] & 0x3f) << 6) | (bytes[index++] & 0x3f);
             }
             else {
                 // four bytes
                 charCode = ((byte & 0x07) << 18) | ((bytes[index++] & 0x3f) << 12) | ((bytes[index++] & 0x3f) << 6) | (bytes[index++] & 0x3f);
             }

             str += chr(charCode);
         }

         return str;
     }

     return {
         stringToBytes: stringToBytes,
         bytesToString: bytesToString
     };
 }());

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
	var secretKey = Base58.decode(privateKey)
	var keyPair = nacl.sign.keyPair.fromSeed(secretKey);
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
	var secretKeyByte = Base58.decode(privateKey);
  var seckey_len = secretKeyByte.length
	var publicKeyByte = Base58.decode(publicKey);
	var secretKeyFull = new Uint8Array(64);
  for (var i = 0; i < seckey_len; i++) {
    secretKeyFull[i] = secretKeyByte[i];
    secretKeyFull[seckey_len + i] = publicKeyByte[i];
  }
	/*----------- convert the msg(string) to msg(Uint8Array ) ---------*/
	var msgByte = utf8.stringToBytes(msg);
//	var msgByte = EncryptUtils.stringToBytes(msg);
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
	var sigByte = Base58.decode(sig);
	var publicKeyByte = Base58.decode(publicKey);
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
    	}while(ch);
		// add stack contents to result
		// done because chars have "wrong" endianness
		re = re.concat( st.reverse() );
	}
	// return an array of bytes
	return re;
};
})(typeof module !== 'undefined' && module.exports ? module.exports : (self.EncryptUtils = self.EncryptUtils || {}));
