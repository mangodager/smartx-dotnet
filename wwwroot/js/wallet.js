(function (Wallet) {
'use strict';

Wallet.Hex2Bytes = function (str) {
    var pos = 0;
    var len = str.length;
    if (len % 2 != 0) {
        return null;
    }
    len /= 2;
    var hexA = new Array();
    for (var i = 0; i < len; i++) {
        var s = str.substr(pos, 2);
        var v = parseInt(s, 16);
        if (v >= 127) v = v - 255 - 1
        hexA.push(v);
        pos += 2;
    }

    var uintarr = new Uint8Array(hexA.length);
    for (var i = 0; i < hexA.length; i++) {
        uintarr[i] = hexA[i];
    }
    return uintarr;
};

Wallet.Bytes2Hex = function(arr) {
    var uintarr = new Uint8Array(arr.length);
    for (var i = 0; i < arr.length; i++) {
        uintarr[i] = arr[i];
    }

    var str = "";
    for (var i = 0; i < uintarr.length; i++) {
        var tmp = uintarr[i].toString(16);
        if (tmp.length == 1) {
            tmp = "0" + tmp;
        }
        str += tmp;
    }
    return str;
};

Wallet.Str2Hex = function (str) {
    if (str === "")
        return "";
    var array = Wallet.Str2Bytes(str);
    return Wallet.Bytes2Hex(array);
};

Wallet.Byte2Str = function(arr) {
    return Base58.encode(arr);
};

Wallet.Str2Bytes = function (str) {
    var ch, st, re = [];
    for (var i = 0; i < str.length; i++) {
        ch = str.charCodeAt(i);  // get char   
        st = [];                 // set up "stack"  
        do {
            st.push(ch & 0xFF);  // push byte to stack  
            ch = ch >> 8;          // shift value down by 1 byte  
        }
        while (ch);
        // add stack contents to result  
        // done because chars have "wrong" endianness  
        re = re.concat(st.reverse());
    }
    // return an array of bytes  
    return re;
};

Wallet.ToAddress = function(publicKey) {
    var publicKeyHex = Wallet.Bytes2Hex(publicKey);
    // ToAddress
    var sha256 = new Hashes.SHA256().hex(publicKeyHex);
    var rmd160 = new Hashes.RMD160().hex(sha256);
    rmd160 = Wallet.Hex2Bytes(rmd160);

    var temp = new Uint8Array(21);
    temp[0] = 1;
    for (var i = 0; i < 20; i++) {
        temp[i + 1] = rmd160[i];
    }

    var data = Wallet.Bytes2Hex(temp);
    // Base58CheckEncode
    var hash1 = new Hashes.SHA256().hex(data);
    var hash2 = new Hashes.SHA256().hex(hash1);
    hash2 = Wallet.Hex2Bytes(hash2);

    var buffer = new Uint8Array(25);
    for (var i = 0; i < temp.length; i++) {
        buffer[i] = temp[i];
    }
    for (var i = 0; i < 4; i++) {
        buffer[21 + i] = hash2[i];
    }
    var b58 = Base58.encode(buffer);

    return b58;
};

Wallet.CheckAddress = function (address) {
    try{
        var decode58 = Base58.decodeArray(address);
        var encode58 = Base58.encode(decode58);

        var temp = new Uint8Array(decode58.length-4);
        for (var i = 0; i < temp.length; i++) {
            temp[i] = decode58[i];
        }
        var data = Wallet.Bytes2Hex(temp);
        var hash1 = new Hashes.SHA256().hex(data);
        var hash2 = new Hashes.SHA256().hex(hash1);
        hash2 = Wallet.Hex2Bytes(hash2);

        for (var i = 0; i < 4; i++) {
            if (decode58[21 + i] != hash2[i]) {
                return false;
            }
        }
        return true;
    }
    catch
    {
    }
    return false;
}


Wallet.CreateKeyPair = function (randomText) {
    var seed = EncryptUtils.generateSeed();

    var temp = Wallet.Byte2Str(seed) + "#" + randomText;
    temp = Wallet.Str2Hex(temp)
    var sha256 = new Hashes.SHA256().hex(temp);

    temp = Wallet.Hex2Bytes(sha256)
    for (var i = 0; i < seed.length; i++) {
        seed[i] = temp[i];
    }
    var KeyPair = EncryptUtils.generateKeyPairSeed(seed);
    KeyPair.randomSeed = seed;
    return KeyPair;
};

//
Wallet.ImportKeyPair = function (mnemonicWord) {
    var seed = Wallet.Hex2Bytes(mnemonicWord);
    var numArr = new Uint8Array(32);
    for (var i = 0; i < seed.length; i++) {
        numArr[i] = seed[i];
    }
    var KeyPair = EncryptUtils.generateKeyPairSeed(numArr);
    KeyPair.randomSeed = seed;
    return KeyPair;
};


//
Wallet.GetMnemonicWord = function (KeyPair) {
    return Wallet.Bytes2Hex(KeyPair.randomSeed);
};

//
Wallet.sign = function (data, keyPair) {
    var dataBytes = Wallet.Hex2Bytes(data)
    var sign = EncryptUtils.sign(keyPair.privateKey, dataBytes);
    var buffer = new Uint8Array(sign.length + keyPair.publicKey.length);
    for (var i = 0; i < sign.length; i++) {
        buffer[i] = sign[i];
    }
    for (var i = 0; i < keyPair.publicKey.length; i++) {
        buffer[i + sign.length] = keyPair.publicKey[i];
    }
    return buffer;
};

Wallet.verify = function (sign,data, address) {
    var dataBytes = Wallet.Hex2Bytes(data)
    var buffer    = new Uint8Array(sign.length - 32);
    var publicKey = new Uint8Array(32);
    for (var i = 0; i < buffer.length; i++) {
        buffer[i] = sign[i];
    }
    for (var i = 0; i < keyPair.publicKey.length; i++) {
        publicKey[i] = sign[i + buffer.length];
    }

    if (EncryptUtils.verify(dataBytes, sign, keyPair.publicKey)) {
        if (Wallet.ToAddress(keyPair.publicKey) == address) {
            return true;
        }
    }
    return false;
};

//
Wallet.Save = function (index,KeyPair,password) {
    if (password != null) {
        var MnemonicWord = Wallet.GetMnemonicWord(KeyPair)
        var ciphertext = CryptoJS.AES.encrypt(MnemonicWord, password).toString();// Encrypt

        localStorage.setItem("KeyPair.MnemonicWord_" + index, ciphertext);
    }
};

//
Wallet.Load = function (index,password) {
    if (password != null) {
        var ciphertext = localStorage.getItem("KeyPair.MnemonicWord_" + index);
        if (ciphertext != null && ciphertext != "") {
            var bytes = CryptoJS.AES.decrypt(ciphertext, password);// Decrypt
            var MnemonicWord = bytes.toString(CryptoJS.enc.Utf8);
            return Wallet.ImportKeyPair(MnemonicWord);
        }
    }
    return null;
};

Wallet.Clear = function () {
    for (var index=1; index < 100; index++) {
        localStorage.removeItem("KeyPair.MnemonicWord_" + index);
    }
    localStorage.removeItem("PasswordHash");
    sessionStorage.removeItem("wallet_password");
};

Wallet.LoadFromAddress = function (addressIn,password) {
    var addressKeyPair = null;
    for (var index = 1; index < 100; index++) {
        var KeyPair = Wallet.Load(index, password);
        if (KeyPair == null)
            break;

        var address = Wallet.ToAddress(KeyPair.publicKey);
        if (address == addressIn) {
            addressKeyPair = KeyPair
        }
    }
    return addressKeyPair;
}

Wallet.GetCount = function (password) {
    var index = 1;
    for (; index < 100; index++) {
        var KeyPair = Wallet.Load(index, password);
        if (KeyPair == null)
            break;
    }
    return index;
}

//
Wallet.Test = function () 
{
    var keyPair = Wallet.ImportKeyPair("aa306f7fad8f12dad3e7b90ee15af0b39e9eccd1aad2e757de2d5ad74b42b67a");
    var data = "e33b68cd7ad3dc29e623e399a46956d54c1861c5cd1e5039b875811d2ca4447d";
    console.warn(data);

    console.warn(Wallet.GetMnemonicWord(keyPair));
    console.warn(Wallet.Bytes2Hex(keyPair.publicKey));
    console.warn(Wallet.Bytes2Hex(keyPair.privateKey));
    console.warn(Wallet.ToAddress(keyPair.publicKey));

    var sign = Wallet.sign(data, keyPair)
    console.warn(Wallet.Bytes2Hex(sign));


};


})(typeof module !== 'undefined' && module.exports ? module.exports : (self.Wallet = self.Wallet || {}));