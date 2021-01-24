# smartx-dotnet
The redesign and re implementation of smartx is different from the consensus and storage of java version, which is implemented by C#

# 中文开发日志：

# 2020-10-29 项目公告
Smartx-dotnet的竞价分片链挖矿教程
https://smartx.one/archives/4387
满足云服务器和长时间运行（1-2个月）的条件之后，可以先学习教程。等学习好了，之后跟群主申请100w的测试币

# 2020-10-31 项目公告
节点程序一些bug，导致10-31号的时候beruler的时候加不到POS出块列表中，目前已经修复，请大家按教程重复试下。

# 2020-11-01 项目公告
想做竞价节点先下载节点1.3版本。然后先更新一下bin补丁，解决1031日拉块和无法加入rukes列表中的问题，直接覆盖，不需要所有人更新。等1.4版本出来了所有人再更新。

# 2020-11-08 项目公告
今天进行第一次竞价分片链轮换测试。排名第24名分片可能会因为持币量没有第25名多，会被系统轮换而无法出块（目前测试网竞价分片第24名地址a7kj1r4ugMGXko48f9WkkEv3ipa5CgVWZ持币量为110万）

# 2020-11-09 项目公告
smartx. net的测试网1.4版本重磅发布，主要修复了k桶广播，拉块，开放者页面文件登录，矿池自己给自己转账，广播达到需确认等问题，下载地址
https://github.com/mangodager/smartx-dotnet/releases/download/1.4/smartx-dotnet-alpha-1.4.zip

# 2020-11-12 项目公告
有两家第三方的社区矿池已经支持了smartx智图PoW矿池挖矿，欢迎大家测试，地址分别是，jnpool.com，smartx.news，目前smartx的PoW矿池数达到3家

# 2020-11-13
测试网1.5.0于11-14日重磅发布，核心是支持智能合约和合约模板商店，届时测试网数据会被清理，请用户做好准备。

# 2020-11-14 项目公告
smartx-dotnet测试网1.5.2智能合约版本如约而至，重磅来袭，超越用户预期，支持一键发智能合约和合约模板，已同步更新github下载
https://github.com/mangodager/smartx-dotnet/releases/download/1.5.2X-dotnet.1.5.2.zip

# 2020-11-14 项目公告
smartx-dotnet测试网1.5.2智能合约版本如约而至，重磅来袭，超越用户预期，支持一键发智能合约和合约模板，已同步更新github下载
https://github.com/mangodager/smartx-dotnet/releases/download/1.5.2X-dotnet.1.5.2.zip

# 2020-11-16 项目公告
smartx-dotnet测试网1.5.2a版本发布，优化了智能合约版本拉块速度和p2p广播，已同步更新github下载
https://github.com/mangodager/smartx-dotnet/releases/download/1.5.2aX-dotnet.1.5.2a.zip

# 2020-11-21 项目公告
普通用户发行代币遇到的问题大多数总结3点：
1、权限问题，没用使用管理员权限运行CMD窗口或者右键管理员权限运行SmartX-dotnet.bat批处理文件
2、同步没完成，查看当前高度:
https://pool.smartx.one/#/
本地高度必须运行到最新高度，才能显示你发行的代币
3、发行代币时，nonce值必须是默认，不能修改

# 2020-11-27 项目公告
目前smartx-dotnet测试网高度停止增长以修复bug，在测试阶段这个属于正常情况，请用户周知。
———————————————
# 2020-11-27 项目公告
修复bug，更新smarx-dotnet1.5.2b版本，直接在1.5.2a基础上打1.5.2补丁，修复出块时，cpu占用过高的问题。
———————————————
# 2020-11-28 项目公告
smartx-dotnet测试网更新到1.5.3版本，超过120个高度不出块的竞价节点会自动退出验证网络，后续需要手动加入。
———————————————
# 2020-11-28 项目公告
这两天版本更新比较多，如果用户发现有什么问题，请到团队论坛中反馈，https://bbs.smartx.one/
此外最新版本1.5.3a测试版本正式发布，已同步github下载https://github.com/mangodager/smartx-dotnet/releases/download/1.5.3aX-dotnet-1.5.3a.zip
1、优化了同步拉块逻辑
2、超过130个高度竞价验证分片不出块，会自动退出rules列表，需要手动再加入
———————————————-
# 2020-12-03项目公告
smartx-dotnet升级了一个最新版本1.5.3b，兼容1.5.3a版本，如果拉块速度不慢，可以不用更新。主要优化：
1、多协程拉块，拉块速度增加几倍
2、优化内存，cpu占用
3、因为锁原因异常报错
已经开放github下载
https://github.com/mangodager/smartx-dotnet/releases/download/1.5.3b/smartx-dotnet-1.5.3b.zip
———————————————
# 2020-12-07项目公告
测试网1.5.3b版本在测试其他用例，暂时停掉团队PoW矿池pool.smartx.one，请周知！
———————————————
# 2020-12-09 项目公告
测试网1.5.3b了修复一些bug，前几天停止了testnet.smartx.one网站服务节点138节点，今天已重启恢复。用户目前可以正常访问.
———————————————
# 2020-12-11 项目公告
smartx-dotnet-1.5.3c版本正式发布
1、这是一个比较重要的版本
2、版本可以合并因自私挖矿或者网络分裂时产生的孤链合并到主链上
3、改善内部管理了http接口的请求速度
———————————————
# 2020-12-16 项目公告
最新珊瑚版主网1.2.5版本正式发布
1、支持新的UI样式
2、支持使用私钥导入并且转账
———————————————
# 2020-12-22 项目公告
smartx-dotnet-1.5.3c版本已经完成分叉链mergechain测试和PoW矿池轮换测试，目前已经开放正常测试网挖矿测试，官方挖矿的ip/地址和矿池网站地址没有变化，请周知。
———————————————
# 2020-12-23 项目公告
smartx-dotnet-1.5.3c版本已经上线最新的区块浏览器和web钱包，地址：https://testnet.smartx.one/browser   和   https://testnet.smartx.one
请周知
———————————————
# 2020-12-30
第三方钱包已支持智图，官网是：
https://hebe.cc/sat/sat.htmlat.html
———————————————
# 2021-01-06 项目公告
smartx-dotnet-1.5.3f版本发布，修复共识bug
———————————————
# 2021-01-11 项目公告
smartx-dotnet-1.5.3g版本发布，更新测试网对账功能。启动测试网对账和交易数据核对，为正式上主网进行先期准备。
———————————————
# 2021-01-19 项目公告1
smartx-dotnet-2.0版本发布，更新"分片链"合约质押流动性挖矿，去中心化质押和随时存取。
———————————————
# 2021-01-19 项目公告2
smartx-dotnet-2.0版本发布，如果有测试币，可以使用测试币在http://pos.smartx.one:8101节点进行抵押流动性挖矿。
———————————————
# 2021-01-20 项目公告
smartx-dotnet-2.0.1版本发布，19号数据已经清理。修改了pos和pow的奖励方式。pos的手续费提2%，然后众筹调为300w，另外pos出块调整为12个，pow调整为72个
如果有测试币，可以使用测试币在http://pos.smartx.one:8101节点进行抵押流动性挖矿。
质押合约挖矿教程：https://bbs.smartx.one/t/topic/548
———————————————
# 2021-01-21 项目公告
smartx-dotnet-2.0.2版本发布，修复了satswap交易所的一些bug，并且测试数据已经清理。请知悉。


