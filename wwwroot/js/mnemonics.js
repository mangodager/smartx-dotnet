(function (mnemonics) {
    'use strict';

    mnemonics.encode = function (hex) {

        console.warn(hex);
        console.warn(hex.length);

        var temp = Wallet.Hex2Bytes(hex);
        console.warn(temp);
        console.warn(temp.length);

        var str = "";
        for (var ii = 0; ii < temp.length; ii++) {
            var bin = temp[ii].toString(2);
            while (bin.length < 8) {
                bin = "0" + bin;
            }
            str += bin;
        }

        console.warn(str);
        console.warn(str.length);

        //var uintarr = {};

        //for (var ii = 0; ii < length; ii++) {
        //    var index = ii / 11;
        //    var jj    = ii % 11;
        //    if (uintarr[index]==null)
        //        uintarr[index] = new Uint8Array(11);
        //    uintarr[index][jj] = temp[jj];
        //}

        //var str = "";
        //for (var ii = 0; ii < uintarr.length; ii++) {
        //    str += english[parseInt(uintarr[ii])];
        //    if (ii + 1 < uintarr.length)
        //        str += " ";
        //}
        //return str;
    };

    mnemonics.decode = function (str) {
        var array = str.split(' ');
        var uintarr = new Uint8Array(128+11);

        for (var ii = 0; ii < array.length; ii++) {
            var jj = 0;
            for (; jj < english.length; jj++) {
                if (english[jj] == array[ii])
                    break;
            }
            uintarr


        }

    };

})(typeof module !== 'undefined' && module.exports ? module.exports : (self.mnemonics = self.mnemonics || {}));











