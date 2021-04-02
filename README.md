# smartx-dotnet
The redesign and re implementation of smartx is different from the consensus and storage of java version, which is implemented by C#

# 中文开发日志：

### 2020-10-29 项目公告
Smartx-dotnet的竞价分片链挖矿教程
https://smartx.one/archives/4387
满足云服务器和长时间运行（1-2个月）的条件之后，可以先学习教程。等学习好了，之后跟群主申请100w的测试币

### 2020-10-31 项目公告
节点程序一些bug，导致10-31号的时候beruler的时候加不到POS出块列表中，目前已经修复，请大家按教程重复试下。

### 2020-11-01 项目公告
想做竞价节点先下载节点1.3版本。然后先更新一下bin补丁，解决1031日拉块和无法加入rukes列表中的问题，直接覆盖，不需要所有人更新。等1.4版本出来了所有人再更新。

### 2020-11-08 项目公告
今天进行第一次竞价分片链轮换测试。排名第24名分片可能会因为持币量没有第25名多，会被系统轮换而无法出块（目前测试网竞价分片第24名地址a7kj1r4ugMGXko48f9WkkEv3ipa5CgVWZ持币量为110万）

### 2020-11-09 项目公告
smartx. net的测试网1.4版本重磅发布，主要修复了k桶广播，拉块，开放者页面文件登录，矿池自己给自己转账，广播达到需确认等问题，下载地址
https://github.com/mangodager/smartx-dotnet/releases/download/1.4/smartx-dotnet-alpha-1.4.zip

### 2020-11-12 项目公告
有两家第三方的社区矿池已经支持了smartx智图PoW矿池挖矿，欢迎大家测试，地址分别是，jnpool.com，smartx.news，目前smartx的PoW矿池数达到3家

### 2020-11-13 项目公告
测试网1.5.0于11-14日重磅发布，核心是支持智能合约和合约模板商店，届时测试网数据会被清理，请用户做好准备。
测试网1.5.0版本是一个支持智能合约和合约商店的大版本
1、支持使用Lua语言创建SmartX智能合约
2、新增SmartX应用商店支持
3、新增1号合约模板-ERC20
4、本地开发者Web工具新增智能合约和合约模板支持
5、使用BigFloat表示内部金额数据，将最小单位由小数点后4个0变成8个0
6、测试网Web钱包新增BigFloat支持和1.5.0版本支持
7、corl命令钱包支持测试网1.5.0版本
8、改进测试网1.4版本的P2P广播模型，快速的块广播送达
9、新增P2P节点能力状态
10、如果竞价节点在120个高度以内没有出块，则会被排除出竞价节点列表，用户需要手动再加入
11、矿池历史数据清理和显示矿池本地算力

### 2020-11-14 项目公告
smartx-dotnet测试网1.5.2智能合约版本如约而至，重磅来袭，超越用户预期，支持一键发智能合约和合约模板，已同步更新github下载
https://github.com/mangodager/smartx-dotnet/releases/download/1.5.2X-dotnet.1.5.2.zip

### 2020-11-14 项目公告
smartx-dotnet测试网1.5.2智能合约版本如约而至，重磅来袭，超越用户预期，支持一键发智能合约和合约模板，已同步更新github下载
https://github.com/mangodager/smartx-dotnet/releases/download/1.5.2X-dotnet.1.5.2.zip

### 2020-11-16 项目公告
smartx-dotnet测试网1.5.2a版本发布，优化了智能合约版本拉块速度和p2p广播，已同步更新github下载
https://github.com/mangodager/smartx-dotnet/releases/download/1.5.2aX-dotnet.1.5.2a.zip

### 2020-11-21 项目公告
普通用户发行代币遇到的问题大多数总结3点：
1、权限问题，没用使用管理员权限运行CMD窗口或者右键管理员权限运行SmartX-dotnet.bat批处理文件
2、同步没完成，查看当前高度:
https://pool.smartx.one/#/
本地高度必须运行到最新高度，才能显示你发行的代币
3、发行代币时，nonce值必须是默认，不能修改

### 2020-11-27 项目公告
目前smartx-dotnet测试网高度停止增长以修复bug，在测试阶段这个属于正常情况，请用户周知。

### 2020-11-27 项目公告
修复bug，更新smarx-dotnet1.5.2b版本，直接在1.5.2a基础上打1.5.2补丁，修复出块时，cpu占用过高的问题。

### 2020-11-28 项目公告
smartx-dotnet测试网更新到1.5.3版本，超过120个高度不出块的竞价节点会自动退出验证网络，后续需要手动加入。

### 2020-11-28 项目公告
这两天版本更新比较多，如果用户发现有什么问题，请到团队论坛中反馈，https://bbs.smartx.one/
此外最新版本1.5.3a测试版本正式发布，已同步github下载https://github.com/mangodager/smartx-dotnet/releases/download/1.5.3aX-dotnet-1.5.3a.zip
1、优化了同步拉块逻辑
2、超过130个高度竞价验证分片不出块，会自动退出rules列表，需要手动再加入

### 2020-12-03 项目公告
smartx-dotnet升级了一个最新版本1.5.3b，兼容1.5.3a版本，如果拉块速度不慢，可以不用更新。主要优化：
1、多协程拉块，拉块速度增加几倍
2、优化内存，cpu占用
3、因为锁原因异常报错
已经开放github下载
https://github.com/mangodager/smartx-dotnet/releases/download/1.5.3b/smartx-dotnet-1.5.3b.zip

### 2020-12-07 项目公告
测试网1.5.3b版本在测试其他用例，暂时停掉团队PoW矿池pool.smartx.one，请周知！

### 2020-12-09 项目公告
测试网1.5.3b了修复一些bug，前几天停止了testnet.smartx.one网站服务节点138节点，今天已重启恢复。用户目前可以正常访问.

### 2020-12-11 项目公告
smartx-dotnet-1.5.3c版本正式发布
1、这是一个比较重要的版本
2、版本可以合并因自私挖矿或者网络分裂时产生的孤链合并到主链上
3、改善内部管理了http接口的请求速度

### 2020-12-16 项目公告
最新珊瑚版主网1.2.5版本正式发布
1、支持新的UI样式
2、支持使用私钥导入并且转账

### 2020-12-22 项目公告
smartx-dotnet-1.5.3c版本已经完成分叉链mergechain测试和PoW矿池轮换测试，目前已经开放正常测试网挖矿测试，官方挖矿的ip/地址和矿池网站地址没有变化，请周知。

### 2020-12-23 项目公告
smartx-dotnet-1.5.3c版本已经上线最新的区块浏览器和web钱包，地址：https://testnet.smartx.one/browser   和   https://testnet.smartx.one
请周知

### 2020-12-30 项目公告
第三方钱包已支持智图，官网是：
https://hebe.cc/sat/sat.htmlat.html

### 2021-01-06 项目公告
smartx-dotnet-1.5.3f版本发布，修复共识bug

### 2021-01-11 项目公告
smartx-dotnet-1.5.3g版本发布，更新测试网对账功能。启动测试网对账和交易数据核对，为正式上主网进行先期准备。

### 2021-01-19 项目公告1
smartx-dotnet-2.0版本发布，更新"分片链"合约质押流动性挖矿，去中心化质押和随时存取。

### 2021-01-19 项目公告2
smartx-dotnet-2.0版本发布，如果有测试币，可以使用测试币在http://pos.smartx.one:8101节点进行抵押流动性挖矿。

### 2021-01-20 项目公告
smartx-dotnet-2.0.1版本发布，19号数据已经清理。修改了pos和pow的奖励方式。pos的手续费提2%，然后众筹调为300w，另外pos出块调整为12个，pow调整为72个
如果有测试币，可以使用测试币在http://pos.smartx.one:8101节点进行抵押流动性挖矿。
质押合约挖矿教程：https://bbs.smartx.one/t/topic/548

### 2021-01-21 项目公告
smartx-dotnet-2.0.2版本发布，修复了satswap交易所的一些bug，并且测试数据已经清理。请知悉。

### 2021-03-25 项目公告
smartx-dotnet-2.0.2b版本发布。
1、改进智图代币对账功能
2、区块浏览器代币显示
3、区块浏览器的代币转账交易排序功能

## 2021-04-02 项目公告
1、测试新的动态Satdag挖矿算法
2、增加质押合约转移功能，转移合约需要万一手续费
3、自动beruler
4、POS节点限制必须创建质押合约才能成为出块节点
5、合约存储优化
6、增加锁仓合约功能
7、增加默认转账手续费0.002个SAT
8、优化全网PoW算力统计和单台机器PoW算法统计
9、增加合约对账功能
10、创建交易对需要100个SAT

# English development logger

### 2020-10-29 Project announcement
Smartx-dotnet's bidding shard chain mining tutorial
https://smartx.one/archives/4387
After meeting the conditions of cloud server and long-term operation (1-2 months), you can study the tutorial first. After studying, apply for 100w test coin with the group owner

### 2020-10-31 Project announcement
There are some bugs in the node program, which caused beruler to not be added to the POS block list on 10-31. It has been repaired so far, please follow the tutorial and try again.

### 2020-11-01 Project announcement
If you want to be a bidding node, download node 1.3 version first. Then first update the bin patch to solve the problem of pulling blocks and being unable to be added to the rukes list on 1031, covering it directly, without requiring everyone to update. Everyone will update when version 1.4 is out.

### 2020-11-08 Project announcement
Today, the first auction shard chain rotation test is conducted. The 24th-ranked shard may not have as many tokens as the 25th, and it will be rotated by the system and cannot be produced (currently the 24th address of the testnet bidding shard a7kj1r4ugMGXko48f9WkkEv3ipa5CgVWZ holds 1.1 million coins)

### 2020-11-09 Project announcement
Smartx.net’s testnet version 1.4 was released. It mainly fixes k-bucket broadcasting, block pull, opener page file login, mining pool transfers to themselves, and broadcasting reaches the need to confirm issues, download address
https://github.com/mangodager/smartx-dotnet/releases/download/1.4/smartx-dotnet-alpha-1.4.zip

### 2020-11-12 Project announcement
Two third-party community mining pools have already supported smartx Zhitu PoW mining pool mining. Welcome to test. The addresses are jnpool.com and smartx.news. At present, the number of smartx PoW mining pools has reached 3

### 2020-11-13 Project announcement
Testnet 1.5.0 was released on 11-14. The core is to support smart contracts and contract template stores. At that time, testnet data will be cleaned up. Users are requested to be prepared.

### 2020-11-14 Project announcement
The smartx-dotnet testnet 1.5.2 smart contract version is coming as scheduled. It hits hard and surpasses user expectations. It supports one-click smart contract and contract template. It has been updated and downloaded on github.
https://github.com/mangodager/smartx-dotnet/releases/download/1.5.2X-dotnet.1.5.2.zip

### 2020-11-14 Project announcement
The smartx-dotnet testnet 1.5.2 smart contract version is coming as scheduled. It hits hard and surpasses user expectations. It supports one-click smart contract and contract template. It has been updated and downloaded on github.
https://github.com/mangodager/smartx-dotnet/releases/download/1.5.2X-dotnet.1.5.2.zip

### 2020-11-16 Project announcement
The smartx-dotnet testnet 1.5.2a version is released, which optimizes the block pulling speed and p2p broadcast of the smart contract version, and has been updated synchronously on github download
https://github.com/mangodager/smartx-dotnet/releases/download/1.5.2aX-dotnet.1.5.2a.zip

### 2020-11-21 Project announcement
Most of the problems encountered by ordinary users in issuing tokens are summarized in 3 points:
1. Permission problem, it is useless to run the CMD window with administrator permissions or right-click the administrator permissions to run the SmartX-dotnet.bat batch file
2. The synchronization is not completed, check the current altitude:
https://pool.smartx.one/#/
The local altitude must run to the latest altitude to display your issued tokens
3. When issuing tokens, the nonce value must be the default and cannot be modified

### 2020-11-27 Project announcement
At present, the height of the smartx-dotnet test network has stopped growing to fix bugs. This is a normal situation during the test phase, please let users know.

### 2020-11-27 Project announcement
Fix bugs, update smarx-dotnet 1.5.2b version, apply the 1.5.2 patch directly on the basis of 1.5.2a, and fix the problem of high CPU usage during block production.

### 2020-11-28 Project announcement
The smartx-dotnet test network is updated to version 1.5.3. More than 120 bidding nodes that do not produce blocks will automatically exit the verification network, and they need to be added manually in the future.

### 2020-11-28 Project announcement
There are a lot of version updates in the past two days. If users find any problems, please feedback in the team forum, https://bbs.smartx.one/
In addition, the latest version 1.5.3a test version is officially released, and has been synchronized with github download https://github.com/mangodager/smartx-dotnet/releases/download/1.5.3aX-dotnet-1.5.3a.zip
1. Optimized the logic of synchronous pull block
2. If more than 130 highly bid verification shards do not produce blocks, they will automatically exit the rules list and need to be added manually

### 2020-12-03 Project announcement
smartx-dotnet has upgraded to the latest version 1.5.3b, which is compatible with version 1.5.3a. If the pulling speed is not slow, you don’t need to update it. Main optimization:
1. Multi-coroutine pull block, the block pull speed increases several times
2. Optimize memory, CPU usage
3. Abnormal error report due to lock
Open github download
https://github.com/mangodager/smartx-dotnet/releases/download/1.5.3b/smartx-dotnet-1.5.3b.zip

### 2020-12-07 Project announcement
The testnet 1.5.3b version is testing other use cases, and the team PoW mining pool pool.smartx.one is temporarily stopped. Please know!

### 2020-12-09 Project announcement
Testnet 1.5.3b has fixed some bugs. A few days ago, the testnet.smartx.one website service node 138 node was stopped, and it has been restarted today. Users can currently access normally.

### 2020-12-11 Project announcement
smartx-dotnet-1.5.3c version officially released
1. This is a more important version
2. The version can be merged into the main chain due to the isolated chain generated by selfish mining or network split
3. Improve the request speed of the internally managed http interface

### 2020-12-16 Project announcement
The latest Coral version mainnet 1.2.5 version is officially released
1. Support new UI styles
2. Support using private key to import and transfer

### 2020-12-22 Project announcement
The smartx-dotnet-1.5.3c version has completed the fork chain mergechain test and the PoW mining pool rotation test. The normal testnet mining test has been opened. The official mining ip/address and the mining pool website address have not changed, please let us know.

### 2020-12-23 Project announcement
The smartx-dotnet-1.5.3c version has launched the latest block explorer and web wallet, address: https://testnet.smartx.one/browser and https://testnet.smartx.one
Please know

### 2020-12-30 Project announcement
The third-party wallet has supported Zhitu, the official website is:
https://hebe.cc/sat/sat.htmlat.html

### 2021-01-06 Project announcement
smartx-dotnet-1.5.3f version released, fix consensus bug

### 2021-01-11 Project announcement
The smartx-dotnet-1.5.3g version was released, and the testnet reconciliation function was updated. Start testnet reconciliation and transaction data verification to prepare for the official launch of the mainnet.

### 2021-01-19 Project Announcement 1
The smartx-dotnet-2.0 version is released, and the "shard chain" contract pledges liquidity mining, decentralized pledge and access at any time.

### 2021-01-19 Project Announcement 2
The smartx-dotnet-2.0 version is released. If there is a test coin, you can use the test coin to conduct mortgage liquidity mining at the node http://pos.smartx.one:8101.

### 2021-01-20 Project announcement
The smartx-dotnet-2.0.1 version is released, and the 19th data has been cleaned up. Modified the reward method of pos and pow. The handling fee of pos is increased by 2%, then the crowdfunding is adjusted to 300w, the block generation of pos is adjusted to 12, and the pow is adjusted to 72
If there is a test coin, you can use the test coin to conduct mortgage liquidity mining at http://pos.smartx.one:8101 node.
Pledge contract mining tutorial: https://bbs.smartx.one/t/topic/548

### 2021-01-21 Project announcement
The smartx-dotnet-2.0.2 version is released, which fixes some bugs of the satswap exchange, and the test data has been cleaned up. Please note.

### 2021-03-25 Project announcement
1. Improve the reconciliation function of Zhitu token
2. Token display in block browser
3. Token transfer transaction sorting function of block browser

## 2021-04-02 Project announcement
1. Test new dynamic satdag mining algorithm
2. The transfer function of pledge contract is added, and the transfer contract needs handling fee
3. Automatic beruler
4. POS node limits that a pledge contract must be created to become a block out node
5. Contract storage optimization
6. Add lock in contract function
7. Increase default transfer fee by 0.002 sat
8. Optimization of pow power statistics of the whole network and POW algorithm statistics of single machine
9. Add contract reconciliation function
10. It takes 100 sats to create a trading pair
