(function (Login) {
'use strict';

    Login.checkPassword = function (password) {
        var temp = Wallet.Str2Hex(password)
        var sha256 = new Hashes.SHA256().hex(temp);
        var PasswordHash = localStorage.getItem("PasswordHash");
        if (PasswordHash!=null&&PasswordHash==sha256)
            return true;
        return false;
    }
    //
    Login.Create = function (password) {
        var temp = Wallet.Str2Hex(password)
        var sha256 = new Hashes.SHA256().hex(temp);
        localStorage.setItem("PasswordHash", sha256);
    };

    //
    Login.SavePassword = function (password) {
        sessionStorage.setItem("wallet_password", password);
    };

    //
    Login.LoadPassword = function () {
        var password = sessionStorage.getItem("wallet_password");
        if (password == null || password == "")
            return null;
        return password;
    };

    //
    Login.CreateUserUI = function (state) {
        if (state) {
            //var innerHTML = "<div class=\"container\">\
            //\<div class=\"modal fade\" id=\"ModalCreateUser\" data-backdrop=\"static\"  tabindex=\"-1\" role=\"dialog\" aria-labelledby=\"ModalCreateUserLabel\" aria-hidden=\"true\">\
            //    < div class=\"modal-dialog\">\
            //        <div class=\"modal-content\">\
            //            <div class=\"modal-header\">\
            //                <h4 class=\"modal-title\" id=\"myModalLabel\">\
            //                   创建账号\
            //        </h4>\
            //            </div>\
            //            <div class=\"input-group\">\
            //                <span class=\"input-group-addon\" id=\"basic-addon3\">输入密码</span>\
            //                <input type=\"password\" class=\"form-control\" id=\"PasswprdText1\" aria-describedby=\"basic-addon3\">\
            //            </div>\
            //            <div class=\"input-group\">\
            //                <span class=\"input-group-addon\" id=\"basic-addon3\">再次输入</span>\
            //                <input type=\"password\" class=\"form-control\" id=\"PasswprdText2\" aria-describedby=\"basic-addon3\">\
            //            </div>\
            //            <div class=\"modal-footer\" >\
            //                <button type=\"button\" class=\"btn btn-primary\" onclick=\"Login.OnCreate('PasswprdText')\">\
            //                    创建\
            //                </button>\
            //            </div>\
            //            </div>\
            //        </div>\
            //    </div>\
            //</div>\n";

            $("#ModalCreateUser").modal('show');
        }
        else {

        }
    };

    Login.OnCreate = function (e) {
        var value1 = document.getElementById(e + "1").value;
        var value2 = document.getElementById(e + "2").value;
        if (value1 != value2) {
            alert("两次输入不一样");
        }
        else
        if (value1 == "") {
            alert("不可以为空");
        }
        else {
            Login.Create(value1);
            Login.SavePassword(value1);
            $("#ModalCreateUser").modal('hide');
        }
    }

    //
    Login.LoginUI = function (state) {
        if (state) {
            //var innerHTML = "<div class=\"container\">\
            //\<div class=\"modal fade\" id=\"ModalLogin\" data-backdrop=\"static\"  tabindex=\"-1\" role=\"dialog\" aria-labelledby=\"ModalLoginLabel\" aria-hidden=\"true\">\
            //    < div class=\"modal-dialog\">\
            //        <div class=\"modal-content\" style=\"width: 600px;margin: 0 auto;\">\
            //            <div class=\"modal-header\">\
            //                <h4 class=\"modal-title\" id=\"myModalLabel\">\
            //                   输入密码登录\
            //        </h4>\
            //            </div>\
            //            <div class=\"input-group\">\
            //                <span class=\"input-group-addon\" id=\"basic-addon3\">输入密码</span>\
            //                <input type=\"password\" class=\"form-control\" id=\"PasswprdText\" aria-describedby=\"basic-addon3\">\
            //            </div>\
            //            <div class=\"modal-footer\">\
            //                <button type=\"button\" class=\"btn btn-primary\" onclick=\"Login.OnLogin('PasswprdText')\">\
            //                    登录\
            //                </button>\
            //            </div>\
            //            </div>\
            //        </div>\
            //    </div>\
            //</div>";
            //document.write(innerHTML);

            $("#ModalLogin").modal('show');
        }
        else {
            $("#ModalLogin").modal('hide');
        }
    };

    Login.OnLogin = function (e) {
        var value = document.getElementById(e).value;
        if (Login.checkPassword(value)) {
            Login.SavePassword(value);
            $("#ModalLogin").modal('hide');
            window.location.href = window.location.href;
        }
        else {
            alert("密码错误");
        }
    }

    Login.Init = function () {
        var PasswordHash = localStorage.getItem("PasswordHash");
        var password = Login.LoadPassword();
        if (PasswordHash == null ) {
            Login.CreateUserUI(true);
        }
        else
        if(password==null||!Login.checkPassword(password))
        {
            Login.LoginUI(true);
        }
    }


})(typeof module !== 'undefined' && module.exports ? module.exports : (self.Login = self.Login || {}));