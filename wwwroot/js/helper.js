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
            serverIP = "http://www.SmartX.com:8101";
        return serverIP;
    }

    Helper.SetServerIP = function (serverIP) {
        localStorage.setItem("serverIP", serverIP);
    }

    Helper.GetPoolIP = function () {
        var poolIP = localStorage.getItem("poolIP");
        if (poolIP == null || poolIP == "" || poolIP == 'null')
            poolIP = "http://www.SmartX.com:8101";
        return poolIP;
    }

    Helper.SetPoolIP = function (poolIP) {
        localStorage.setItem("poolIP", poolIP);
    }

    Helper.GetCrossChainRpc = function () {
        return Helper.GetServerIP();
        //return 'http://127.0.0.1:8547';
    }
    Helper.PoolList = {}

    Helper.GetSSFAddress = function () {
        var SSFAddress = localStorage.getItem("SSFAddress");
        if (SSFAddress == null || SSFAddress=="")
            SSFAddress = "dsoZAxn4GEiGycq2sFc24CAQn4SRCgDuS";
        return SSFAddress;
    }

    Helper.SetSSFAddress = function (SSFAddress) {
        localStorage.setItem("SSFAddress", SSFAddress);
    }

    Helper.ERCSat = function () {
        return "RnnUBgzrzv2z7YrEz5ZhuzVtbkCbspKpV";
    }
    
    Helper.PledgeFactory = function () {
        return "SWipqG94LJXXx9E8sYbSpZVa8n5TSUD2B";
    }

    Helper.LockFactory = function () {
        return "RXF5eSnpEGNgRsUZdx9t2o5ByB511NzrT";
    }

    Helper.GetRewardRule = function () {
        //return "2522880000";//GetRewardRule*100
        return "420480000";//GetRewardRule*100
    }

    Helper.GetNFTAddress = function () {
        var NFTAddress = localStorage.getItem("NFTAddress");
        if (NFTAddress == null || NFTAddress == "")
            NFTAddress = "c3MbYezD7CV9KcRZbnoVTmT6SMAzs2REL";
        return NFTAddress;
    }

    Helper.SetNFTAddress = function (NFTAddress) {
        localStorage.setItem("NFTAddress", NFTAddress);
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
        return item_new;
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

    Helper.StatusbarInsert = function () {
        // 删除之前的数据
        var mytableEle = document.getElementById(arguments[0]);
        for (var i = mytableEle.children.length - 1; i >= 0; i--) {
            if (mytableEle.children[i].id.indexOf("_mytable111") == -1)
                mytableEle.children[i].remove();
        }
        if(arguments.length==1) {
            return;
        }

        var innerHTML = "<tbody><tr class='mycolor' id='myid' onclick='Helper.ShowTransfer(event)'>";
        innerHTML = innerHTML.replace(/myid/g, arguments[1]);
        innerHTML += "<td style='text-align:center;vertical-align:middle;'>" + arguments[3] + "</td>";

        if(arguments[4]==1) {
            innerHTML = innerHTML.replace(/mycolor/g, "");
            innerHTML += "<th style='width:10%;height:100%;overflow-x:hidden;text-align:center;'> \
            <img src='./static/waiting.gif'/ style='width:24px;height:24px;'> \
            </th>";
        }
        else
        if(arguments[4]==2) {
            innerHTML = innerHTML.replace(/mycolor/g, "list-group-item-danger");
            innerHTML += "<th style='width:10%;height:100%;overflow-x:hidden;text-align:center;color: #af0000'> "
            Translate.Get("失败") + 
            "</th>";
        }
        else
        if(arguments[4]==3) {
            innerHTML = innerHTML.replace(/mycolor/g, "list-group-item-success");
            innerHTML += "<th style='width:10%;height:100%;overflow-x:hidden;text-align:center;color: #00af05'> "
            Translate.Get("完成") + 
            "</th>";
        }
        else
        if(arguments[4]==4) {
            innerHTML = innerHTML.replace(/mycolor/g, "list-group-item-success");
            innerHTML += "<th style='width:10%;height:100%;overflow-x:hidden;text-align:center;color: #00af05'> "
            Translate.Get("丢失") + 
            "</th>";
        }
        innerHTML += '</tr ></tbody>';

        var item_new = document.createElement("tbody");
        item_new.innerHTML = innerHTML;
        document.getElementById(arguments[0]).appendChild(item_new);
        
        var tableId     = arguments[0];
        var elementById = arguments[1];
        var arguments_2 = arguments[2];
        var arguments_3 = arguments[3];
        var arguments_4 = arguments[4];
        var arguments_5 = arguments[5];
        var timestamp   = arguments[6];
        var timestamp2  = new Date().getTime();
        var timestamp3  = 10000-(timestamp2-timestamp);

        setTimeout(function(){
            if(arguments_4!=1) {
                Helper.Statusbar(elementById);
                return;
            }
            $.ajax({
                url: Helper.GetServerIP() + "/TransferState",
                dataType: "text",
                type: "get",
                data: { hash: elementById },
                success: function (data) {
                    if (data != "") {
                        var jsonObj = JSON.parse(data);
                        let state = 2;
                        if(jsonObj["height"]!=0) {
                            state = 3;
                        }
                        Helper.Statusbar(tableId,elementById,arguments_2,arguments_3,state,arguments_5,null,true);
                    }
                    else {
                        if(arguments_4==1&&timestamp2-timestamp>45000) {
                            Helper.Statusbar(tableId,elementById,arguments_2,arguments_3,4,arguments_5,null,true);
                        }
                        else {
                            Helper.Statusbar(tableId,elementById,arguments_2,arguments_3,1,arguments_5,null,true);
                        }
                    }
                }
            });
        }, timestamp3 > 0 ? timestamp3 : 3000 );
    }

    Helper.Statusbar = function () {
        if (arguments.length == 0) {
            Helper.Redirect();

            Translate.Init();
            var jsonStr   = localStorage.getItem("Statusbar");
            if(jsonStr!=null)
            {
                var arguments2 = JSON.parse(jsonStr);
                Helper.StatusbarInsert(arguments2[0],arguments2[1],arguments2[2],arguments2[3],arguments2[4],arguments2[5],arguments2[6]);
            }
        }
        else
        if(arguments.length==1) {
            var jsonStr   = localStorage.getItem("Statusbar");
            if(jsonStr!=null) {
                var arguments2 = JSON.parse(jsonStr);
                if(arguments2[1]!=arguments[0]) {
                    return;
                }
            }
            localStorage.removeItem("Statusbar");
            Helper.StatusbarInsert(arguments[0]);
            return;
        }
        else {
            if(arguments[6]==null)
                arguments[6] = new Date().getTime();
            if (arguments[7] != null) {
                var jsonStr = localStorage.getItem("Statusbar");
                if (jsonStr != null) {
                    var arguments2 = JSON.parse(jsonStr);
                    if (arguments2[1] != arguments[1]) {
                        return;
                    }
                }
                else {
                    return;
                }
            }

            var jsonStr = JSON.stringify(arguments);
            localStorage.setItem("Statusbar", jsonStr);
            
            Helper.StatusbarInsert(arguments[0],arguments[1],arguments[2],arguments[3],arguments[4],arguments[5],arguments[6]);
            }

    }

    Helper.Redirect = function ()
    {
        if (window.location.href.indexOf("Redirect=1") != -1)
            return;
        $.ajax({
            url: Helper.GetServerIP() + "/Redirect",
            dataType: "text",
            type: "get",
            data: { style: "2" },
            success: function (data) {
                if (data.indexOf("redirect ") == 0) {
                    // 重定向
                    window.location.href = data.split(" ")[1] + "/pool.html?Redirect=1&poolUrl=http://" + window.location.host;
                    return;
                }
            },
            error: function (err) {
            }
            });
    }

    Helper.MessageBox = function ()
    {
        if (arguments.length == 1 && arguments[0] != null) {
            if (document.body.item_new != null) {
                document.body.removeChild(document.body.item_new);
            }

            // ModalMessageBox
            var innerHTML = "\
                <div class='modal fade' id='ModalMessageBox' data-backdrop='static' tabindex='-1' role='dialog' aria-labelledby='ModalMessageLabel' aria-hidden='true' style='top:10%;'>\
                    <div class='modal-dialog' style='width:460px;margin-top: 20%;'>\
                        <div class='modal-content' data-dismiss='modal'>\
                            <form>\
                                <ul style='padding-left:0px;'>\
                                    <div style='height: 30px; user-select:none'></div>\
                                    <center><h4 style=' user-select:none'>######&nbsp;&nbsp;<img src='./static/waiting.gif'/ style='width:24px;height:24px;margin-top:-2px;'></h4>\</center>\
                                    <div style='height: 20px; user-select:none'></div>\
                                </ul>\
                            </form>\
                        </div>\
                    </div>\
                </div>\
            ";
            innerHTML = innerHTML.replace(/######/g, arguments[0]);
            
            document.body.item_new = document.createElement("div");
            document.body.item_new.innerHTML = innerHTML;
            document.body.appendChild(document.body.item_new);

            $("#ModalMessageBox").modal('show');
            setTimeout(function(){$("#ModalMessageBox").modal('hide');},"1000");
        }
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

    Helper.GetTransferByHash = function (hash) {
        for (var index = 1; index < 100; index++) {
            var KeyPair = Wallet.Load(index, Login.LoadPassword());
            if (KeyPair == null)
                break;

            var address = Wallet.ToAddress(KeyPair.publicKey);
            var count = Helper.GetTransferCount(address);

            var jj = 1;
            for (; jj < 100; jj++) {
                var str = Helper.LoadTransfer(address, jj);
                if (str == null)
                    break;
                var jsonObj = JSON.parse(str);
                if (jsonObj == null)
                    break;
                if (jsonObj["hash"] == hash) {
                    return str;
                }
            }
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

    Helper.SendTransferSubmit = function (e, type, amount, addressOut, data, depend, nonce, remarks, serverIP)
    {
        if (Login.LoadPassword() == null) {
            alert("password is null!");
            return null;
        }
        // 取秘钥
        var addressKeyPair = Wallet.LoadFromAddress(addressCur, Login.LoadPassword());
        if (addressKeyPair == null) {
            alert("Please import the Keystore first!");
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

        if (type =="contract"&&data.indexOf("transfer")!=-1)
            Helper.AddTransfer(addressCur + addressOut, jsonStr);
        else
            Helper.AddTransfer(addressCur, jsonStr);

        if (serverIP == null || serverIP == "")
            serverIP = Helper.GetServerIP()
        $.ajax({
            url: serverIP + "/Transfer",
            dataType: "text",
            type: "get",
            data: transferdata,
            success: function (data) {
                var jsonObj = JSON.parse(data);
                if (data.indexOf("success\":true")==-1) {
                    var text = "";
                    var rel = jsonObj["rel"];
                    switch (rel) {
                        case -1:
                            text = Translate.Get("节点无出块权限");
                            break;
                        case -2:
                            text = Translate.Get("验签错误");
                            break;
                        case -3:
                            text = Translate.Get("余额小于0.002,无法扣除手续费");
                            break;
                        case -4:
                            text = Translate.Get("转出账户不存在");
                            break;
                        case -5:
                            text = Translate.Get("转出余额不足");
                            break;
                        case -6:
                            text = Translate.Get("小数点后位数超过8");
                            break;
                        case -7:
                            text = Translate.Get("节点拥堵");
                            break;
                        case -8:
                            text = Translate.Get("出入地址相同");
                            break;
                        case -9:
                            text = Translate.Get("交易已存在");
                            break;
                        case -10:
                            text = Translate.Get("转入地址无效");
                            break;
                        case -11:
                            text = Translate.Get("交易包大小超过限制");
                            break;
                    }
                    alert(Translate.Get("提交失败: ") + text);
                    Helper.liOnTransferInfoDelete(hash);
                }

                //alert("提交成功!");
                if(e!=null) {
                    $("#" + e).modal("hide");
                    //window.location.href = window.location.href;
                }
            },
            error: function (err) {
                alert(Translate.Get("网络连接错误!"));
            }
        });
        return hash;
    };

    Helper.Mul = function (aa,bb) {
        if(typeof(aa)=='string')
            aa = new BigNumber(aa.replace(/,/g, ""));
        if(typeof(bb)=='string')
            bb = new BigNumber(bb.replace(/,/g, ""));
        return aa.multipliedBy(bb).toFormat(8).replace(/,/g, "");
    }
    Helper.Div = function (aa,bb) {
        if(typeof(aa)=='string')
            aa = new BigNumber(aa.replace(/,/g, ""));
        if(typeof(bb)=='string')
            bb = new BigNumber(bb.replace(/,/g, ""));
        return aa.dividedBy(bb).toFormat(8).replace(/,/g, "");
    }
    Helper.Add = function (aa,bb) {
        if(typeof(aa)=='string')
            aa = new BigNumber(aa.replace(/,/g, ""));
        if(typeof(bb)=='string')
            bb = new BigNumber(bb.replace(/,/g, ""));
        return aa.plus(bb).toFormat(8).replace(/,/g, "");
    }
    Helper.Sub = function (aa,bb) {
        if(typeof(aa)=='string')
            aa = new BigNumber(aa.replace(/,/g, ""));
        if(typeof(bb)=='string')
            bb = new BigNumber(bb.replace(/,/g, ""));
        return aa.minus(bb).toFormat(8).replace(/,/g, "");
    }
    Helper.Fix = function (aa) {
        if(aa==null||aa=="")
            aa = "0"
        if(typeof(aa)=='string')
            aa = new BigNumber(aa.replace(/,/g, ""));
        aa = new BigNumber(aa.toFormat(8).replace(/,/g, ""));
        if (aa == "NaN")
            aa = new BigNumber(0);
        if (aa == "Infinity")
            aa = new BigNumber(0);
        return aa.toFormat().replace(/,/g, "");
    }

    Helper.CheckAmount = function (aa,bb,cc) {
        aa = Helper.Fix(aa);
        bb = Helper.Fix(bb);

        if(aa==""||aa=="NaN") {
            alert(Translate.Get("请输入数额"));
            return false;
        }
        if(new BigNumber(aa).isLessThanOrEqualTo(new BigNumber("0"))) {
            alert(Translate.Get("数额必须大于0"));
            return false;
        }
        //if(aa!="0"&&new BigNumber(aa).isGreaterThan(new BigNumber(bb))) {
        //    alert("超出余额: "+bb);
        //    return false;
        //}
        if(cc!=null&&new BigNumber(aa).isLessThan(new BigNumber(cc))) {
            alert(Translate.Get("数额必须大于: ")+cc);
            return false;
        }
        return true;
    }

    Helper.appendTransferModal = function() {
        var TransferInfo = document.getElementById("TransferInfo")
        if(TransferInfo==null)
        {
            var innerHTML = "\
                <div class=\"modal fade\" id=\"TransferInfo\" data-backdrop=\"static\" tabindex=\"-1\" role=\"dialog\" aria-labelledby=\"TransferInfoLabel\" aria-hidden=\"true\" style=\"top:10%\">\
                    <div class=\"modal-dialog\">\
                        <div class=\"modal-content\">\
                            <div class=\"modal-header\">\
                                <button type=\"button\" class=\"close\" data-dismiss=\"modal\" aria-hidden=\"true\">×</button>\
                                <h4 class=\"modal-title\" id=\"myModalLabel\">Transfer</h4>\
                            </div>\
                            <div style='width:100%;height:100%;overflow-x:auto;'>\
                                <table class='table table-hover' id='TransferInfoTable'>\
                                    <tbody>\
                                        <thead id=\"thead_mytable111\">\
                                            <tr>\
                                                <th>key</th>\
                                                <th>value</th>\
                                            </tr>\
                                        </thead>\
                                    </tbody>\
                                </table>\
                            </div>\
                            <div class=\"modal-footer\">\
                                <button type=\"button\" class=\"btn btn-primary\" onclick=\"Helper.liOnTransferInfoResend(event)\" id = \"TransferInfoResend\" >\
                                    <div class=\"lang\" key=\"重发\"></div>\
                                </button>\
                                <button type=\"button\" class=\"btn btn-primary\" onclick=\"Helper.liOnTransferInfoDelete(event)\">\
                                    <div class=\"lang\" key=\"删除\"></div>\
                                </button>\
                                <button type=\"button\" class=\"btn btn-primary\" onclick=\"Helper.liOnTransferInfoLeave(event)\">\
                                    <div class=\"lang\" key=\"离开\"></div>\
                                </button>\
                            </div>\
                        </div>\
                    </div>\
                </div>\
            ";

            var item_new = document.createElement("div");
            item_new.innerHTML = innerHTML;
            document.body.appendChild(item_new);
            $("#TransferInfo").modal('hide');

            Translate.Init();
        }
    }

    Helper.ShowTransfer = function(text) {
        Helper.appendTransferModal();
        if(typeof(text)!='string') {
            text = text.currentTarget.id;
        }
        
        if (text != "") {
            $.ajax({
                url: Helper.GetServerIP() + "/TransferState2",
                dataType: "text",
                type: "get",
                data: { hash: text },
                success: function (data) {
                    colorindex = 1;
                    if (data != "") {
                        // 删除之前的数据
                        var mytableEle = document.getElementById("TransferInfoTable");
                        for (var i = mytableEle.children.length - 1; i >= 0; i--) {
                            if (mytableEle.children[i].id.indexOf("_mytable111") == -1)
                                mytableEle.children[i].remove();
                        }

                        var jsonObj = JSON.parse(data);
                        for (var key in jsonObj) {
                            var color = colorlist[(colorindex - 1) % colorlist.length]; colorindex++;
                            if (key == "timestamp") {
                                var tempValue = jsonObj[key];
                                if (typeof (tempValue) == 'object')
                                    tempValue = JSON.stringify(tempValue);
                                Helper.TableInsert("TransferInfoTable", key, color, key, tempValue + " (" + Helper.formatDate(tempValue) + ")")
                            }
                            else
                            if (key != "linksblk" && key != "linkstran") {
                                Helper.TableInsert("TransferInfoTable", key, color, key, jsonObj[key])
                            }
                        }
                        var color = colorlist[(colorindex - 1) % colorlist.length]; colorindex++;

                        let state = Translate.Get("交易失败");
                        if (jsonObj["height"] != 0) {
                            state = Translate.Get("交易已完成");
                        }
                        else {
                            if (jsonObj["temp"] != null && jsonObj["temp"][jsonObj["temp"].length - 1] == "Transfer In Queue") {
                                state = Translate.Get("Transfer In Queue");
                            }
                            else
                            if (jsonObj["temp"] != null && jsonObj["temp"][jsonObj["temp"].length - 1] == "Waiting for block confirmation") {
                                state = Translate.Get("Waiting for block confirmation");
                            }
                        }

                        Helper.TableInsert("TransferInfoTable", "state", color, Translate.Get("状态"), state)
                        document.curshottransferHash = text;
                        $("#TransferInfo").modal('show');
                        $('#TransferInfoResend').attr("disabled",true);
                    }
                    else {
                        // transferWait
                        var str = Helper.GetTransfer(addressCur + tokenAddress, text);
                        if(str==null)
                            str = Helper.GetTransferByHash(text);

                        var jsonObj = JSON.parse(str);
                        
                        // 删除之前的数据
                        var mytableEle = document.getElementById("TransferInfoTable");
                        for (var i = mytableEle.children.length - 1; i >= 0; i--) {
                            if (mytableEle.children[i].id.indexOf("_mytable111") == -1)
                            mytableEle.children[i].remove();
                        }
                        
                        for (var key in jsonObj) {
                            var color = colorlist[(colorindex - 1) % colorlist.length]; colorindex++;
                            if (key != "linksblk" && key != "linkstran") {
                                Helper.TableInsert("TransferInfoTable", key, color, key, jsonObj[key])
                            }
                        }
                        
                        var state = Translate.Get("交易未确认");
                        try {
                            if (new Date().getTime()-parseInt(jsonObj["timestamp"]) > 75000)
                                state = Translate.Get("交易丢失");
                        }
                        catch{}
                        
                        var color = colorlist[(colorindex - 1) % colorlist.length]; colorindex++;
                        Helper.TableInsert("TransferInfoTable", "state", color, Translate.Get("状态"), state)
                        document.curshottransferHash = text;
                        $("#TransferInfo").modal('show');
                        if (state == Translate.Get("交易丢失") && (document.ResendlastTime==null||(new Date().getTime())-document.ResendlastTime>30000) ) {
                            $('#TransferInfoResend').attr("disabled",false);
                        }
                        else {
                            $('#TransferInfoResend').attr("disabled",true);
                        }
                    }
                },
                error: function (err) {
                    alert(Translate.Get("网络连接错误!"));
                }
            });
        }
    };

    // 删除处理中的交易
    Helper.liOnTransferInfoDelete = function (hash) {
        if (hash == null)
            hash = document.curshottransferHash;
        if (hash != "") {
            Helper.Statusbar(hash);
            Helper.DelTransfer(addressCur + tokenAddress, hash)
            document.curshottransferHash = "";
            //window.location.href = window.location.href;
            $("#TransferInfo").modal('hide');
        }
    };

    Helper.liOnTransferInfoLeave  = function(e) {
        $("#TransferInfo").modal('hide');
    };

    Helper.liOnTransferInfoResend = function(evevt) {
        let e = "TransferInfo";
        if (document.curshottransferHash != "") {       
            if(document.ResendlastTime!=null&&(new Date().getTime())-document.ResendlastTime<30000)
            {
                return;
            }
            document.ResendlastTime = new Date().getTime();

            var str = Helper.GetTransfer(addressCur + tokenAddress,document.curshottransferHash);
            var transferdata = JSON.parse(str);
            if(transferdata.nonce<nonceCur) {
                alert(Translate.Get("transfer nonce 已失效!"));
                return;
            }

            $.ajax({
                url: Helper.GetServerIP() + "/Transfer",
                dataType: "text",
                type: "get",
                data: transferdata,
                success: function (data) {
                    var jsonObj = JSON.parse(data);
                    if (data != "{\"success\":true}") {
                        alert(Translate.Get("提交失败,交易数据出错或者节点无出块权限: ") + jsonObj["rel"]);
                        Helper.liOnTransferInfoDelete(document.curshottransferHash);
                    }
                    if(e!=null) {
                        alert(Translate.Get("交易已重发!"));
                        $("#" + e).modal("hide");
                        //window.location.href = window.location.href;
                    }
                },
                error: function (err) {
                    alert(Translate.Get("网络连接错误!"));
                }
            });
        }
    }

    Helper.getByID = function (parentID,myID) {
        var myIDEle = $('#' + myID);
        var mytableEle = $('#' + parentID);
        var children = mytableEle.find(myID);

        return children;
    };

    Helper.StringFormat = function () {
        if (arguments.length == 0) {
            return "";
        }
        if (arguments.length == 1) {
            return arguments[0];
        }
        var result = arguments[0];
        for (var ii = 1; ii < arguments.length; ii++) {
            var value = arguments[ii];
            if (null != value) {
                var reg2 = new RegExp("({)" + (ii - 1) + "(})", "g");
                result = result.replace(reg2, value);
            }
        }
        return result;
    }

    Helper.loadScript = function (url, callback) {
        var script = document.createElement("script")
        script.type = "text/javascript";
        if (script.readyState) { //IE
            script.onreadystatechange = function () {
                if (script.readyState == "loaded" || script.readyState == "complete") {
                    script.onreadystatechange = null;
                    if (callback!=null)callback();
                }
            };
        } else { //Others
            script.onload = function () {
                if (callback != null)callback();
            };
        }
        script.src = url;
        document.head.appendChild(script);
    }

    Helper.formatDate = function(date) {
        var date = new Date(date);
        var YY = date.getFullYear() + '-';
        var MM = (date.getMonth() + 1 < 10 ? '0' + (date.getMonth() + 1) : date.getMonth() + 1) + '-';
        var DD = (date.getDate() < 10 ? '0' + (date.getDate()) : date.getDate());
        var hh = (date.getHours() < 10 ? '0' + date.getHours() : date.getHours()) + ':';
        var mm = (date.getMinutes() < 10 ? '0' + date.getMinutes() : date.getMinutes()) + ':';
        var ss = (date.getSeconds() < 10 ? '0' + date.getSeconds() : date.getSeconds());
        return YY + MM + DD + " " + hh + mm + ss;
    }

})(typeof module !== 'undefined' && module.exports ? module.exports : (self.Helper = self.Helper || {}));