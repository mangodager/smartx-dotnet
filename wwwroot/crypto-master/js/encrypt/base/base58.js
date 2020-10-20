var bs58alphabet = '123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz';
var bs58 = BaseX(bs58alphabet);

Base58 = {
    encode: function(source) {
        if (typeof source == 'string') {
            var buffer = [];
            for (var i = 0; i < source.length; i++) {
                buffer.push(source.charCodeAt(i));
            }

            return this.encode(buffer);
        }

        return bs58.encode(source);
    },
    decode: function(source) {
        return String.fromCharCode.apply(source, bs58.decode(source));
    },
    decodeArray: function(source) {
        return bs58.decode(source);
    }
};
