(function (Helper) {
'use strict';

    Helper.checkPlatform = function() {
        var system = {};
        var p = navigator.platform;
        system.win = p.indexOf('Win') == 0;
        system.mac = p.indexOf("Mac") == 0;
        system.x11 = (p == "X11") || (p.indexOf("Linux") == 0);
        if (system.win || system.mac || system.x11) {
            return "pc";
        } else {
            return "phone";
        }
    }

    Helper.GetServerIP = function () {
        var serverIP = localStorage.getItem("serverIP");
        if (serverIP == null || serverIP=="")
            serverIP = "http://www.SmartX.com:8004";
        return serverIP;
    }

    Helper.SetServerIP = function (serverIP) {
        localStorage.setItem("serverIP", serverIP);
    }

    Helper.TableInsert = function () {
        var innerHTML = "<tbody><tr class='mycolor' id='myid' onclick='liOnclick(this)'>";
        innerHTML = innerHTML.replace(/myid/g, arguments[1]);
        innerHTML = innerHTML.replace(/mycolor/g, arguments[2]);

        for (var i = 3; i < arguments.length; i++) {
            var str = "<td >" + arguments[i] + "</td>"
            innerHTML += str;
        }
        innerHTML += '</tr ></tbody>';

        var item_new = document.createElement("tbody");

        item_new.innerHTML = innerHTML;
        document.getElementById(arguments[0]).appendChild(item_new);
    }

    Helper.TableInsert2 = function () {
        var innerHTML = "<tbody><tr class='mycolor' id='myid' onclick='liOnclick(this)'>";
        innerHTML = innerHTML.replace(/myid/g, arguments[1]);
        innerHTML = innerHTML.replace(/mycolor/g, arguments[2]);

        innerHTML += "<td >" + arguments[3] + "</td>";
        innerHTML += "<td style='text-align:right;'>" + arguments[4] + "</td.amount>";
        innerHTML += '</tr ></tbody>';

        var item_new = document.createElement("tbody");

        item_new.innerHTML = innerHTML;
        document.getElementById(arguments[0]).appendChild(item_new);
    }

    Helper.TableInsert3 = function () {
        var innerHTML = "<tbody><tr class='mycolor' id='myid' onclick='liOnclick(this)'>";
        innerHTML = innerHTML.replace(/myid/g, arguments[1]);
        innerHTML = innerHTML.replace(/mycolor/g, arguments[2]);

        innerHTML += "<td >" + arguments[3] + "</td>";
        innerHTML += "<td >" + arguments[4] + "</td>";
        innerHTML += "<td style='text-align:right;'>" + arguments[5] + "</td.amount>";
        innerHTML += '</tr ></tbody>';

        var item_new = document.createElement("tbody");

        item_new.innerHTML = innerHTML;
        document.getElementById(arguments[0]).appendChild(item_new);
    }

    Helper.MyTableCount = function (name) {
        var mytableEle = document.getElementById(name);
        return mytableEle.children.length - 2;
    }

    Helper.TableInsertNoSelect2 = function () {
        var innerHTML = "<tbody><tr class='mycolor' id='myid' onclick='liOnclick(this)'>";
        innerHTML = innerHTML.replace(/myid/g, arguments[1]);
        innerHTML = innerHTML.replace(/mycolor/g, arguments[2]);

        innerHTML += "<td >" + arguments[3] + "</td>";
        innerHTML += "<td class='noselect' style='text-align:right;'>" + arguments[4] + "</td.amount>";
        innerHTML += '</tr ></tbody>';

        var item_new = document.createElement("tbody");

        item_new.innerHTML = innerHTML;
        document.getElementById(arguments[0]).appendChild(item_new);
    }

    Helper.Simplify = function (str, platform) {
        if (platform == "pc") {
            return str;
        }
        return str.slice(0, 12) + "..." + str.slice(str.length - 6, str.length)
    }


    Helper.LoadTransfer = function (address,index) {
        return localStorage.getItem(address + "_transfer_" + index);
    }

    Helper.AddTransfer = function (address,transfer) {
        var count = Helper.GetTransferCount(address);
        localStorage.setItem(address + "_transfer_" + (count+1), transfer);
    }

    Helper.DelTransfer = function (address, hash) {
        var count = Helper.GetTransferCount(address);

        var index = 1;
        for (; index < 100; index++) {
            var str = Helper.LoadTransfer(address, index);
            if (str == null)
                return;
            var jsonObj = JSON.parse(str);
            if (jsonObj == null)
                return;
            if (jsonObj["hash"] == hash) {
                if (count > 1 && index != count) {
                    var transfer = localStorage.getItem(address + "_transfer_" + count);
                    localStorage.setItem(address + "_transfer_" + index, transfer);
                    localStorage.removeItem(address + "_transfer_" + count);
                }
                else {
                    localStorage.removeItem(address + "_transfer_" + index);
                }
                return;
            }
        }
    }

    Helper.GetTransfer = function (address, hash) {
        var count = Helper.GetTransferCount(address);

        var jj = 1;
        for (; jj < 100; jj++) {
            var str = Helper.LoadTransfer(address, jj);
            if (str == null)
                return;
            var jsonObj = JSON.parse(str);
            if (jsonObj == null)
                return;
            if (jsonObj["hash"] == hash) {
                return str;
            }
        }
    }


    Helper.GetTransferCount = function (address) {
        var index = 1;
        for (; index < 100; index++) {
            var KeyPair = Helper.LoadTransfer(address,index);
            if (KeyPair == null)
                break;
        }
    return index-1;
    }

    Helper.ClearTransferWait = function (address) {
        var index = 1;
        for (; index < 100; index++) {
            localStorage.removeItem(address + "_transfer_" + index);
        }
    }

   /** 下载钱包 */
    Helper.funDownload = function (content, filename) {

        /** 创建隐藏的可下载链接 */
        let eleLink = document.createElement('a');

        eleLink.download = filename;

        eleLink.style.display = 'none';

        /** 字符内容转变成blob地址 */
        let blob = new Blob([content]);

        eleLink.href = URL.createObjectURL(blob);

        /** 触发点击 */
        document.body.appendChild(eleLink);

        eleLink.click();

        /** 然后移除 */
        document.body.removeChild(eleLink);
    };

    Helper.SendTransferSubmit = function (e, type, amount, addressOut, data, depend, nonce, remarks)
    {
        if (Login.LoadPassword() == null) {
            alert("password is null!");
            return;
        }
        // 取秘钥
        var addressKeyPair = Wallet.LoadFromAddress(addressCur, Login.LoadPassword());
        if (addressKeyPair == null) {
            window.location.href = "index.html";
            return;
        }

        var timestamp = new Date().getTime();
        var hashdata = type + "#" + nonce + "#" + addressCur + "#" + addressOut + "#" + amount + "#" + data + "#" + depend + "#" + timestamp + "#" + remarks;
        var hash = new Hashes.SHA256().hex(hashdata);
        var sign = Wallet.sign(hash, addressKeyPair);
        var signHex = Wallet.Bytes2Hex(sign);

        var transferdata = { type: type, hash: hash, nonce: nonce, addressIn: addressCur, addressOut: addressOut, amount: amount, data: data, depend: depend, timestamp: timestamp, sign: signHex, remarks: remarks }
        //console.warn(hash);
        //console.warn(sign);

        var jsonStr = JSON.stringify(transferdata);

        if (type =="contract")
            Helper.AddTransfer(addressCur + addressOut, jsonStr);
        else
            Helper.AddTransfer(addressCur, jsonStr);

        $.ajax({
            url: Helper.GetServerIP() + "/Transfer",
            dataType: "text",
            type: "get",
            data: transferdata,
            success: function (data) {
                var jsonObj = JSON.parse(data);
                if (data != "{\"success\":true}") {
                    alert("提交失败,交易数据出错或者节点无出块权限: " + jsonObj["rel"]);
                }

                //alert("提交成功!");
                $("#" + e).modal("hide");
                window.location.href = window.location.href;
            },
            error: function (err) {
                alert("网络连接错误!");
            }
        });

    };


})(typeof module !== 'undefined' && module.exports ? module.exports : (self.Helper = self.Helper || {}));