
Storages = {}

function create(_name,_symbol,_tokenA,amountInA,_factory,_tokenC)
	if Storages.publisher==nil then
		Storages.publisher    = sender;
		Storages.name         = _name;
		Storages.symbol       = _symbol;
		Storages.decimals     = 8;
		Storages.totalSupply  = 0;

		Storages.factory   = _factory
		Storages.tokenA    = _tokenA
		Storages.tokenC    = _tokenC
		Storages.reserveA  = 0
		Storages.blockTimestampLast = 0

		addLiquidity(amountInA,amountInA);

        lualib.TransferEvent(sender, "", "addLiquidity: " .. addressThis);
	end
end

--返回ERC20代币的名字
function name()
	return Storages.name;
end

--返回代币的简称
function symbol()
	return Storages.symbol;
end

--返回token使用的小数点后几位。比如如果设置为3,就是支持0.001表示。
function decimals()
	return Storages.decimals;
end

--返回token的总供应量
function totalSupply()
	return Storages.totalSupply;
end

--返回某个地址(账户)的账户余额
function balanceOf(_owner)
	return luaDB.GetValue("balances",_owner);
end

function liquidityOf(_owner,_factory)
	lualib.Assert( Storages.factory == _factory , "PledgePair: DIFF_FACTORY" )
	local balanceA = lualib.BalanceOf(Storages.tokenA,addressThis);

	local value_owner = luaDB.GetValue("balances",_owner);
	local relA = biglib.Less(value_owner,"0",true) and _owner ~= Storages.publisher and allowance(_owner)[0] == false;
	relA = relA ~= true;

	return addressThis,Storages.tokenA,Storages.tokenA,balanceA,balanceA,value_owner,Storages.totalSupply,relA;
end

--从代币合约的调用者地址上转移 _value的数量token到的地址 _to
function transfer(_to, _value)
	local value_sender = luaDB.GetValue("balances",sender);
	local value_to     = luaDB.GetValue("balances",_to);

	lualib.Assert( sender ~= _to , "transfer: sender == _to" );
	lualib.Assert( biglib.Greater(value_sender,_value,true) , "transfer: balances not enough" );
	lualib.Assert( biglib.Greater( biglib.Add(value_to , _value) , value_to,true) , "transfer: balances overflow" );

	value_sender = biglib.Sub(value_sender,_value);
	value_to     = biglib.Add(value_to,_value);

	luaDB.SetValue("balances",sender,value_sender);
	luaDB.SetValue("balances",_to,value_to);

	lualib.TransferEvent(sender, _to, _value);
	return true;
end

function getReserves()
	return Storages.reserveA,Storages.reserveA;
end

function addLiquidity(amountADesired,amountAMin,deadline)
	local amountA = amountADesired

	-- 更新Storages.reserveA
	_update(lualib.BalanceOf(Storages.tokenA,addressThis),nil)

	lualib.TransferToken(Storages.tokenA,sender,addressThis,amountA);

	local liquidity = _mint(sender);
	--print(rapidjson.encode(Storages) );

	lualib.TransferEvent(sender, "", "addLiquidity: " .. liquidity);
end

function __mint(_to,value)
	Storages.totalSupply  = biglib.Add(Storages.totalSupply,value);

	local value_to = luaDB.GetValue("balances",_to);
    value_to = biglib.Add(value_to,value);
	luaDB.SetValue("balances",_to,value_to);
end

function _mint(_to)
	--lualib.Assert()
    local balanceA = lualib.BalanceOf(Storages.tokenA,addressThis);
	local amountA = biglib.Sub(balanceA,Storages.reserveA);
	
	local  liquidity = "0";
	local  MINIMUM_LIQUIDITY = "0"; --"3000";
	if biglib.Equals(Storages.totalSupply,"0") then
		liquidity = amountA;
		--print("mint 1 = " .. liquidity)
	else
		liquidity = biglib.Div( biglib.Mul(amountA,Storages.totalSupply), Storages.reserveA);
		--print("mint 2 = " .. liquidity)
	end

	lualib.Assert( biglib.Greater(liquidity,"0") , "PledgePair: INSUFFICIENT_LIQUIDITY_MINTED" )
	__mint(_to, liquidity);

	_update(balanceA, _reserveA);

	return liquidity;
end

function removeLiquidity(liquidity)
	transfer(addressThis,liquidity);
	_burn(liquidity);
end

function _burn(liquidity)

	local reserveA = Storages.reserveA
    local balanceA = lualib.BalanceOf(Storages.tokenA,addressThis);
	local amountA = biglib.Div( biglib.Mul(liquidity,balanceA) , Storages.totalSupply );
	lualib.Assert( biglib.Greater(amountA,"0") , "PledgePair: INSUFFICIENT_LIQUIDITY_BURNED" );

	--lualib.TransferToken(Storages.tokenA,addressThis,sender,amountA);
	local dataA = string.format("approve(\"%s\",\"%s\",%s)",sender,amountA,"207360");
	local relA  = lualib.Call(Storages.tokenC,dataA);
	lualib.Assert( relA , "PledgePair: approveA" );
	
	local balanceA = lualib.BalanceOf(Storages.tokenA,addressThis);

	-- _burn
	local value_addressThis = luaDB.GetValue("balances",addressThis);
    value_addressThis = biglib.Sub(value_addressThis,liquidity);
	luaDB.SetValue("balances",addressThis,value_addressThis);

    Storages.totalSupply = biglib.Sub(Storages.totalSupply,liquidity);
	
	_update(balanceA,reserveA);

end

function _update(balanceA, _reserveA)

	 Storages.reserveA = balanceA;

end

function retrieved(_lockHeight)
	local dataC = string.format("retrieved(\"%s\",\"%s\",%s)",addressThis,sender,_lockHeight);
	local relC = lualib.Call(Storages.tokenC,dataC);
	lualib.Assert( relC , "PledgePair: retrievedC" );

end

function allowance(_spender)
	local dataC = string.format("allowance(\"%s\",\"%s\")",addressThis,_spender);
	return lualib.Call(Storages.tokenC,dataC);
end

function getLockAddress()
	return Storages.tokenC;
end

function addLiquidityTo(_to,amountADesired,amountAMin,deadline)
	lualib.Assert( lualib.IsERC(sender,"PledgePair") or lualib.IsERC(sender,"PledgeLock") , "not a PledgePair or PledgeLock");

	local amountA = amountADesired

	-- 更新Storages.reserveA
	_update(lualib.BalanceOf(Storages.tokenA,addressThis),nil);

	lualib.TransferToken(Storages.tokenA,sender,addressThis,amountA);

	local liquidity = _mint(_to);

	--print(rapidjson.encode(Storages) );
	lualib.TransferEvent("", _to, "addLiquidityTo: " .. liquidity);
end

function diversionLiquidity(liquidity,diversionAddress)
	lualib.Assert( addressThis ~= diversionAddress , "contract same");
	lualib.Assert( lualib.IsERC(diversionAddress,"PledgePair") , "not a PledgePair");
	local dataA    = string.format("getPair(\"%s\")",diversionAddress);
	local factoryA = lualib.Call(Storages.factory,dataA);
	lualib.Assert( lualib.CheckAddress(factoryA[0]) , "diversionAddress error");

	transfer(addressThis,liquidity);

	local reserveA = Storages.reserveA
    local balanceA = lualib.BalanceOf(Storages.tokenA,addressThis);
	local amountA = biglib.Div( biglib.Mul(liquidity,balanceA) , Storages.totalSupply );
	
	-- FeeTo
	local dataB = string.format("getFeeToSetter()");
	local feeToData = lualib.Call(Storages.factory,dataB);
	if feeToData ~= nil and biglib.Greater(feeToData[1],"0") then
		lualib.Assert( lualib.CheckAddress(feeToData[0]) , "publisher error");
		local amountfee = biglib.Mul(amountA,feeToData[1]);
		amountA = biglib.Sub(amountA,amountfee);
		lualib.TransferToken(Storages.tokenA,addressThis,feeToData[0],amountfee);
		lualib.TransferEvent(sender, feeToData[0], "amountfee:" .. amountfee);
	end

	lualib.Assert( biglib.Greater(amountA,"0") , "diversionLiquidity 3" );
	--lualib.TransferToken(Storages.tokenA,addressThis,sender,amountA);
	local dataL = string.format("addLiquidityTo(\"%s\",\"%s\",\"%s\")",sender,amountA,amountA);
	lualib.Call(diversionAddress,dataL);

	local balanceA = lualib.BalanceOf(Storages.tokenA,addressThis);

	-- _burn
	local value_addressThis = luaDB.GetValue("balances",addressThis);
    value_addressThis = biglib.Sub(value_addressThis,liquidity);
	luaDB.SetValue("balances",addressThis,value_addressThis);

    Storages.totalSupply = biglib.Sub(Storages.totalSupply,liquidity);
	
	_update(balanceA,reserveA);

end




