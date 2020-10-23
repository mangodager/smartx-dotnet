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



})(typeof module !== 'undefined' && module.exports ? module.exports : (self.Helper = self.Helper || {}));