﻿(function (Translate) {
'use strict';

    Translate.arrLang = {
        "en": {
            "语言": "中文",
            "确定": "OK",
            "创建": "Create",
            "导入": "Import",
            "导出": "Export",
            "地址": "Address",
            "查找!": "Search!",
            "余额": "Amount",
            "余额:": "Amount:",
            "钱包": "Wallet",
            "区块": "Block",
            "节点": "Ruler",
            "设置": "Setting",
            "正在创建地址": "Creating Address",
            "请输入随机数": "Please input a random number",
            "取消": "Cancel",
            "提交": "Submit",
            "正在导入地址": "Importing address",
            "初始化账本": "Initialization Account",
            "输入密码登录": "input the password to login",
            "输入密码": "input the password",
            "再次输入": "input again",
            "登录": "Sign in",
            "密码错误": "Wrong password",
            "两次输入不一样": "The two inputs are different",
            "不可以为空": "Cannot be empty",
            "保存": "Save",
            "节点http地址:": "Node HTTP address:",
            "刷新": "Refresh",
            "矿池": "Pool",
            "账户:": "Address:",
            "上 一 页": "Previous page",
            "下 一 页": "Next page",
            "账单列表:": "Bill list:",
            "时间": "Time",
            "金额": "Amount",
            "交易 ID": "Transfer ID",
            "状态": "State",
            "编号": "Index",
            "矿机名": "Name",
            "实时算力": "Realtime power",
            "平均算力": "Average power",
            "矿机列表:(平均算力仅供参考，请以实际收益为准)": "Miner List:(The average power is for reference only, please refer to the actual income)",
            "离开": "Close",
            "重发": "ReSend",
            "删除": "Delete",
            "交易失败": "Transfer Failed",
            "交易已完成": "Transfer Finish",
            "交易丢失": "Transfer Lose",
            "收款": "Receiving",
            "扫码支付": "Scan to Pay",
            "转账": "Transfer",
            "复制地址": "Copy Address",
            "发起锁仓": "Start LockPair",
            "清空交易": "Clear Processing",
            "地址已复制到剪切板": "Address copied to clipboard",
            "处理列表:": "Processing list:",
            "正在发起交易": "Creatting Transfer",
            "出块": "Create Block",
            "合约  发布": "Contract  Create",
            "合约  调用": "Contract  Call",
            "矿工费:": "fee:",
            "锁仓时间:": "time:",
            "锁仓名称:": "name:",
            "转出地址:": "addr out:",
            "转账金额:": "amount:",
            "转入地址:": "addr in:",
            "小时数:": "hours:",
            "正在申请锁仓:": "Creatting LockPart:",
            "照相机": "camera",
            "备注:": "remarks:",
            "发布合约": "Create Contract",
            "调用合约": "Call Contract",
            "质押合约": "Pledge",
            "质押合约:": "Pledge:",
            "锁仓": "Locked",
            "名称": "Token",
            "交易": "Transfer",
            "导出 Keystore": "Export Keystore",
            "正在发布智能合约": "Creatting Contract",
            "发布地址:": "addr in:",
            "合约模板:": "template:",
            "参数:": "data:",
            "发布": "Create",
            "种类": "Type",
            "取回状态": "Retrieve state",
            "创建质押合约": "Create Pledge",
            "合约": "Contract",
            "我的质押:": "My Pledge:",
            "资金池": "Cash pool",
            "资金池:": "Cash pool:",
            "年化率": "Annualized rate",
            "总额度": "Total",
            "我的占比": "My Share",
            "我的凭证": "My Certificate",
            "我的资金": "My Amount",
            "质押合约:": "Ruler Pledge:",
            "确认信息": "Confirm Information",
            "您确认要撤销取回吗?": "Are you sure you want to cancel the retrieval?",
            "序号": "Number",
            "取回资产:": "Retrieve:",
            "取回": "Retrieve",
            "取回状态:": "Retrieve State:",
            "添加": "Add",
            "转移": "Move",
            "合约地址:": "Contract:",
            "总额度:": "Total:",
            "年化率:": "Annualized rate:",
            "我的额度:": "My Certificate:",
            "凭证占比:": "My Share:",
            "节点状态:": "Ruler State:",
            "可用凭证:": "valid Certificate:",
            "转移资产:": "Move Capital:",
            "转移至:": "Move To:",
            "质押转移将会扣除手续费:0.01%": "Move the Pledge will cost fee:0.01%",
            "小时后可取回": "hour later Retrievable",
            "分钟后可取回": "minute later Retrievable",
            "小于1分钟后可取回": "1 minutes to Retrievable",
            "撤销": "Cancel",
            "离线": "Offline",
            "良好": "Nice",
            "合约已存在": "The contract already exists",
            "创建质押合约: ": "Create pledge contract: ",
            "创建质押合约已提交": "Create pledge contract submitted",
            "没有找到可用节点": "No available nodes were found",
            "解除质押": "Remove Liquidity",
            "解除质押已提交": "Remove Liquidity Submitted",
            "转移质押": "Move Pledge",
            "转移质押已提交": "Move Pledge Submitted",
            "增加质押": "Add Pledge",
            "增加质押已提交": "Add Pledge Submitted",
            " 取回质押,序号:": " Retrieve Pledge, Number: ",
            "取回质押已提交": "Retrieve Pledge Submitted",
            " 撤销取回,序号: ": " Cancel Retrieve, Number: ",
            "撤销取回已提交": "Cancel Retrieve Submitted",
            "网络连接错误!": "Network connection error!",
            "提交失败: ": "Failed to submit: ",
            "节点无出块权限": "The node has no block permission",
            "验签错误": "Signature verification error",
            "余额小于0.002,无法扣除手续费": "The balance is less than 0.002",
            "转出账户不存在": "Transfer out account does not exist",
            "转出余额不足": "Insufficient transfer out balance",
            "小数点后位数超过8": "More than 8 decimal places",
            "节点拥堵": "Node clogging",
            "出入地址相同": "Same access address",
            "交易已存在": "Transfer already exists",
            "转入地址无效": "Invalid transfer out address",
            "交易包大小超过限制": "Transfer package size exceeds limit",
            "请输入数额": "please enter a number",
            "数额必须大于0": "The number must be greater than 0",
            "数额必须大于: ": "The number must be greater than: ",
            "transfer nonce 已失效!": "transfer nonce is invalid!",
            "提交失败,交易数据出错或者节点无出块权限: ": "Failed to submit , Transaction data error or node has no block permission :",
            "交易已重发!": "The transaction has been reissued!",
            "失败": "Failed",
            "完成": "Finish",
            "丢失": "Lose",
            " 转出: ": " Transfer out: ",
            "交易已提交": "Transfer submitted",
            "发起锁仓: ": "Create Locked: ",
            " 序号: ": " Index: ",
            "发起锁仓已提交": "Create Locked submitted",
            "无法识别,请重试！": "Unrecognized, please try again!",
            "兑换": "Exchange",
            "交易对": "TxPair",
            "输入 :": "Input :",
            "选择代币": "Select",
            "份额": "Share",
            "创建交易对需要支付100SAT": "100sat is required to create a TxPair",
            "创建交易对": "Create TxPair",
            "选择": "Select",
            "添加流动性": "Add Liquidity",
            "我的流动性:": "My Liquidity:",
            "取回流动性": "Retrieve Liquidity",
            "没有找到交易对!": "No Find TxPair!",
            "兑换已提交": "Exchange submitted",
            " 添加流动性: ": " Add Liquidity: ",
            "添加流动性已提交": "Add liquidity submitted",
            "交易对已存在": "TxPair already exists",
            " 创建交易对: ": " Create TxPair: ",
            "创建交易对已提交": "Create TxPair submitted",
            " 取回流行性: ": " Retrieve Liquidity: ",
            "取回流行性已提交": "Retrieve Liquidity submitted",
            "交易未确认": "Transfer not confirmed",
            "(冷启动期间收益只有1/3)": "(only 1/3 of the cold launch)",
            "数量超出限制": "out of limit",
            "已存在": "Already exists",
            "矿机数": "Miner Count",
            "总算力": "Total Power",
            "有效提交": "Effective Share",
            "预计分账": "Amount In Pool",
            "正在转": "Proceeding",
            "矿池名": "Pool Name",
            "申请补发交易": "Application for reissue transaction",
            "数字资产": "NFT",
            "创建数字资产": "Create NFT",
            "元表数据:": "MetaData:",
            "跨链": "CrossChain",
            "跨链BSC": "CrossChainBSC",

        },
        "zh": {
            "语言": "English",
            "确定": "确定",
            "创建": "创建",
            "导入": "导入",
            "导出": "导出",
            "地址": "地址",
            "查找!": "查找!",
            "余额": "余额",
            "余额:": "余额:",
            "钱包": "钱包",
            "区块": "区块",
            "节点": "节点",
            "设置": "设置",
            "正在创建地址": "正在创建地址",
            "请输入随机数": "请输入随机数",
            "取消": "取消",
            "提交": "提交",
            "正在导入地址": "正在导入地址",
            "初始化账本": "初始化账本",
            "输入密码登录": "输入密码登录",
            "输入密码": "输入密码",
            "再次输入": "再次输入",
            "登录": "登录",
            "密码错误": "密码错误",
            "两次输入不一样": "两次输入不一样",
            "不可以为空": "不可以为空",
            "保存": "保存",
            "节点http地址:": "节点http地址:",
            "刷新": "刷新",
            "矿池": "矿池",
            "账户:": "账户:",
            "上 一 页": "上 一 页",
            "下 一 页": "下 一 页",
            "账单列表:": "账单列表:",
            "时间": "时间",
            "金额": "金额",
            "交易 ID": "交易 ID",
            "状态": "状态",
            "编号": "编号",
            "矿机名": "矿机名",
            "实时算力": "实时算力",
            "平均算力": "平均算力",
            "矿机列表:(平均算力仅供参考，请以实际收益为准)": "矿机列表:(平均算力仅供参考，请以实际收益为准)",
            "离开": "离开",
            "重发": "重发",
            "删除": "删除",
            "交易失败": "交易失败",
            "交易已完成": "交易已完成",
            "交易丢失": "交易丢失",
            "收款": "收款",
            "扫码支付": "扫码支付",
            "转账": "转账",
            "复制地址": "复制地址",
            "发起锁仓": "发起锁仓",
            "清空交易": "清空交易",
            "地址已复制到剪切板": "地址已复制到剪切板",
            "处理列表:": "处理列表:",
            "正在发起交易": "正在发起交易",
            "出块": "出块",
            "合约  发布": "合约  发布",
            "合约  调用": "合约  调用",
            "矿工费:": "矿工费:",
            "锁仓时间:": "锁仓时间:",
            "锁仓名称:": "name:",
            "转出地址:": "转出地址:",
            "转账金额:": "转账金额:",
            "转入地址:": "转入地址:",
            "小时数:": "小时数:",
            "正在申请锁仓:": "正在申请锁仓:",
            "照相机": "照相机",
            "备注:": "备注:",
            "发布合约": "发布合约",
            "调用合约": "调用合约",
            "质押合约": "质押合约",
            "质押合约:": "质押合约:",
            "锁仓": "锁仓",
            "名称": "名称",
            "交易": "交易",
            "导出 Keystore": "导出 Keystore",
            "正在发布智能合约": "正在发布智能合约",
            "发布地址:": "发布地址:",
            "合约模板:": "合约模板:",
            "参数:": "参数:",
            "发布": "发布",
            "种类": "种类",
            "取回状态": "取回状态",
            "创建质押合约": "创建质押合约",
            "合约": "合约",
            "我的质押:": "我的质押:",
            "资金池": "资金池",
            "资金池:": "资金池:",
            "年化率": "年化率",
            "总额度": "总额度",
            "我的占比": "我的占比",
            "我的凭证": "我的凭证",
            "我的资金": "我的资金",
            "质押合约:": "质押合约:",
            "确认信息": "确认信息",
            "您确认要撤销取回吗?": "您确认要撤销取回吗?",
            "序号": "序号",
            "取回资产:": "取回资产:",
            "取回": "取回",
            "取回状态:": "取回状态:",
            "添加": "添加",
            "转移": "转移",
            "合约地址:": "合约地址:",
            "总额度:": "总额度:",
            "年化率:": "年化率:",
            "我的额度:": "我的额度:",
            "凭证占比:": "凭证占比:",
            "节点状态:": "节点状态:",
            "可用凭证:": "可用凭证:",
            "转移资产:": "转移资产:",
            "转移至:": "转移至:",
            "质押转移将会扣除手续费:0.01%": "质押转移将会扣除手续费:0.01%",
            "小时后可取回": "小时后可取回",
            "分钟后可取回": "分钟后可取回",
            "小于1分钟后可取回": "小于1分钟后可取回",
            "撤销": "撤销",
            "离线": "离线",
            "良好": "良好",
            "合约已存在": "合约已存在",
            "创建质押合约: ": "创建质押合约: ",
            "创建质押合约已提交": "创建质押合约已提交",
            "没有找到可用节点": "没有找到可用节点",
            "解除质押": "解除质押",
            "解除质押已提交": "解除质押已提交",
            "转移质押": "转移质押",
            "转移质押已提交": "转移质押已提交",
            "增加质押": "增加质押",
            "增加质押已提交": "增加质押已提交",
            " 取回质押,序号:": " 取回质押,序号:",
            "取回质押已提交": "取回质押已提交",
            " 撤销取回,序号: ": " 撤销取回,序号: ",
            "撤销取回已提交": "撤销取回已提交",
            "网络连接错误!": "网络连接错误!",
            "提交失败: ": "提交失败: ",
            "节点无出块权限": "节点无出块权限",
            "验签错误": "验签错误",
            "余额小于0.002,无法扣除手续费": "余额小于0.002,无法扣除手续费",
            "转出账户不存在": "转出账户不存在",
            "转出余额不足": "转出余额不足",
            "小数点后位数超过8": "小数点后位数超过8",
            "节点拥堵": "节点拥堵",
            "出入地址相同": "出入地址相同",
            "交易已存在": "交易已存在",
            "转入地址无效": "转入地址无效",
            "交易包大小超过限制": "交易包大小超过限制",
            "请输入数额": "请输入数额",
            "数额必须大于0": "数额必须大于0",
            "数额必须大于: ": "数额必须大于: ",
            "transfer nonce 已失效!": "transfer nonce 已失效！",
            "提交失败,交易数据出错或者节点无出块权限: ": "提交失败,交易数据出错或者节点无出块权限: ",
            "交易已重发!": "交易已重发!",
            "失败": "失败",
            "完成": "完成",
            "丢失": "丢失",
            " 转出: ": " 转出: ",
            "交易已提交": "交易已提交",
            "发起锁仓: ": "发起锁仓: ",
            " 序号: ": " 序号: ",
            "发起锁仓已提交": "发起锁仓已提交",
            "无法识别,请重试！": "无法识别,请重试！",
            "兑换": "兑换",
            "交易对": "交易对",
            "输入 :": "输入 :",
            "选择代币": "选择代币",
            "份额": "份额",
            "创建交易对需要支付100SAT": "创建交易对需要支付100SAT",
            "创建交易对": "创建交易对",
            "选择": "选择",
            "添加流动性": "添加流动性",
            "我的流动性:": "我的流动性:",
            "取回流动性": "取回流动性",
            "没有找到交易对!": "没有找到交易对!",
            "兑换已提交": "兑换已提交",
            " 添加流动性: ": " 添加流动性: ",
            "添加流动性已提交": "添加流动性已提交",
            "交易对已存在": "交易对已存在",
            " 创建交易对: ": " 创建交易对: ",
            "创建交易对已提交": "创建交易对已提交",
            " 取回流行性: ": " 取回流行性: ",
            "取回流行性已提交": "取回流行性已提交",
            "交易未确认": "交易未确认",
            "(冷启动期间收益只有1/3)": "(冷启动期间收益只有1/3)",
            "数量超出限制": "数量超出限制",
            "已存在": "已存在",
            "矿机数": "矿机数",
            "总算力": "总算力",
            "有效提交": "有效提交",
            "预计分账": "预计分账",
            "正在转": "正在转",
            "矿池名": "矿池名",
            "申请补发交易": "申请补发交易",
            "数字资产": "数字资产",
            "创建数字资产": "创建数字资产",
            "元表数据:": "元表数据:",
            "跨链": "跨链",
            "跨链BSC": "跨链BSC",
        }
    };

    //
    Translate.Init = function () {

        // The default language is English
        var lang = "en";
        // Check for localStorage support
        if ('localStorage' in window) {
            var lang = localStorage.getItem('lang') || navigator.language.slice(0, 2);
            if (!Object.keys(Translate.arrLang).includes(lang)) lang = 'en';
        }

        $(document).ready(function () {
            $(".lang").each(function (index, element) {
                $(this).text(Translate.arrLang[lang][$(this).attr("key")]);
            });
        });

        // get/set the selected language
        $(".translate").click(function () {
            if (lang == "zh")
                lang = "en"
            else
                lang = "zh"

            // update localStorage key
            if ('localStorage' in window) {
                localStorage.setItem('lang', lang);
                console.log(localStorage.getItem('lang'));
            }

            $(".lang").each(function (index, element) {
                $(this).text(Translate.arrLang[lang][$(this).attr("key")]);
            });
        });

    };


    Translate.Get = function (text) {
        // The default language is English
        var lang = "zh";
        // Check for localStorage support
        if ('localStorage' in window) {
            var lang = localStorage.getItem('lang') || navigator.language.slice(0, 2);
            if (!Object.keys(Translate.arrLang).includes(lang)) lang = 'en';
        }

        return Translate.arrLang[lang][text] ?? text;
    }


})(typeof module !== 'undefined' && module.exports ? module.exports : (self.Translate = self.Translate || {}));