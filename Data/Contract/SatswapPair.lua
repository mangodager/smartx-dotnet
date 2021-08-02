
Storages = {}

function create(_name,_symbol,_tokenA,_tokenB,amountInA,amountInB,_publisher)
	if Storages.publisher==nil then
		Storages.publisher   = sender;
		Storages.name        = _name;
		Storages.symbol      = _symbol;
		Storages.decimals    = 8;
		Storages.totalSupply = 0;

		Storages.factory     = _publisher
		Storages.tokenA      = _tokenA
		Storages.tokenB      = _tokenB
		Storages.reserveA    = 0
		Storages.reserveB    = 0
		Storages.blockTimestampLast  = 0

		addLiquidity(amountInA,amountInB,"0","0","");

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

--返回token使用的小数点后几位。比如如果设置为3，就是支持0.001表示。
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
	lualib.Assert( Storages.factory == _factory , "SatswapPair: DIFF_FACTORY" )

	local value_owner = luaDB.GetValue("balances",_owner);
	return addressThis,Storages.tokenA,Storages.tokenB,Storages.reserveA,Storages.reserveB,value_owner,Storages.totalSupply;
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
	return Storages.reserveA,Storages.reserveB;
end

function quote(amountA, reserveA, reserveB)
	return biglib.Div(biglib.Mul(amountA,reserveB),reserveA)
end

function _addLiquidity(amountADesired,amountBDesired,amountAMin,amountBMin)
	if biglib.Equals(Storages.reserveA,"0") and biglib.Equals(Storages.reserveB,"0") then
		return amountADesired,amountBDesired;
	else
		local amountBOptimal = quote(amountADesired,Storages.reserveA,Storages.reserveB);
		if biglib.Less(amountBOptimal,amountBDesired,true) then
			lualib.Assert( biglib.Greater(amountBOptimal,amountBMin,true) , "SatswapPair: INSUFFICIENT_B_AMOUNT" )
			return amountADesired,amountBOptimal;
		else
			local amountAOptimal = quote(amountBDesired,Storages.reserveB,Storages.reserveA);
			lualib.Assert( biglib.Less(amountAOptimal,amountADesired,true) , "SatswapPair: INSUFFICIENT_A_AMOUNT Less" )
			lualib.Assert( biglib.Greater(amountAOptimal,amountAMin,true) , "SatswapPair: INSUFFICIENT_A_AMOUNT Greater" )
			return amountAOptimal,amountBDesired;
		end
	end

	return amountA,amountB;
end

function addLiquidity(amountADesired,amountBDesired,amountAMin,amountBMin,deadline)
	local amountA,amountB = _addLiquidity(amountADesired,amountBDesired,amountAMin,amountBMin)

	lualib.TransferToken(Storages.tokenA,sender,addressThis,amountA);
	lualib.TransferToken(Storages.tokenB,sender,addressThis,amountB);

	local liquidity = _mint();

	--print(rapidjson.encode(Storages) );
	lualib.TransferEvent(sender, "", "addLiquidity: " .. addressThis);
end

function __mint(_to,value)
	Storages.totalSupply  = biglib.Add(Storages.totalSupply,value)
	local value_to = luaDB.GetValue("balances",_to);
    value_to = biglib.Add(value_to,value);
	luaDB.SetValue("balances",_to,value_to);
end

function _mint()
	--lualib.Assert()
    local balanceA = lualib.BalanceOf(Storages.tokenA,addressThis);
    local balanceB = lualib.BalanceOf(Storages.tokenB,addressThis);

	local amountA = biglib.Sub(balanceA,Storages.reserveA);
	local amountB = biglib.Sub(balanceB,Storages.reserveB);

	local  liquidity = "0";
	local  MINIMUM_LIQUIDITY = "0"; --"3000";
	if biglib.Equals(Storages.totalSupply,"0") then
		liquidity = biglib.Sub( biglib.Sqrt( biglib.Mul(amountA,amountB) ) , MINIMUM_LIQUIDITY)
		--print("_mint 1")
	else
		local  liquidityA = biglib.Div( biglib.Mul(amountA,Storages.totalSupply), Storages.reserveA);
		local  liquidityB = biglib.Div( biglib.Mul(amountB,Storages.totalSupply), Storages.reserveB);
		liquidity = biglib.Min( liquidityA, liquidityB);
		--print("_mint 2")
	end

	lualib.Assert( biglib.Greater(liquidity,"0") , "SatswapPair: INSUFFICIENT_LIQUIDITY_MINTED" )
    __mint(sender, liquidity);

	_update(balanceA, balanceB, _reserveA, _reserveB);

	return liquidity;
end

function removeLiquidity(liquidity)
	transfer(addressThis,liquidity);
	_burn(liquidity)

end

function _burn(liquidity)

	local reserveA = Storages.reserveA
	local reserveB = Storages.reserveB

    local balanceA = lualib.BalanceOf(Storages.tokenA,addressThis);
    local balanceB = lualib.BalanceOf(Storages.tokenB,addressThis);

	local amountA = biglib.Div( biglib.Mul(liquidity,balanceA) , Storages.totalSupply );
	local amountB = biglib.Div( biglib.Mul(liquidity,balanceB) , Storages.totalSupply );

	lualib.Assert( biglib.Greater(amountA,"0") and biglib.Greater(amountB,"0") , "SatswapPair: INSUFFICIENT_LIQUIDITY_BURNED" )

	-- _burn
	local value_addressThis = luaDB.GetValue("balances",addressThis);
    value_addressThis = biglib.Sub(value_addressThis,liquidity);
	luaDB.SetValue("balances",addressThis,value_addressThis);

    Storages.totalSupply = biglib.Sub(Storages.totalSupply,liquidity);

	lualib.TransferToken(Storages.tokenA,addressThis,sender,amountA);
	lualib.TransferToken(Storages.tokenB,addressThis,sender,amountB);

    local balance0 = lualib.BalanceOf(Storages.tokenA,addressThis);
    local balance1 = lualib.BalanceOf(Storages.tokenB,addressThis);

	_update(balance0,balance1,reserveA,reserveB)

end

function _getAmountA(amountB,reserveA, reserveB)
	lualib.Assert( biglib.Greater(amountB,"0") , "SatswapPair: INSUFFICIENT_INPUT_AMOUNT" )
	lualib.Assert( biglib.Greater(reserveA,"0") and biglib.Greater(reserveB,"0") , "SatswapPair: INSUFFICIENT_LIQUIDITY" )

	local numerator   = biglib.Mul( biglib.Mul( reserveA , amountB ) , "1000");
	local denominator = biglib.Mul( biglib.Sub( reserveB , amountB ) ,  "997");
	local _amount     = biglib.Add( biglib.Div( numerator, denominator ) , "0.00000001");	

	return _amount;
end

function _getAmountB(amountA,reserveA, reserveB)
	lualib.Assert( biglib.Greater(amountA,"0") , "SatswapPair: INSUFFICIENT_INPUT_AMOUNT" )
	lualib.Assert( biglib.Greater(reserveA,"0") and biglib.Greater(reserveB,"0") , "SatswapPair: INSUFFICIENT_LIQUIDITY" )

	local amountInWithFee = biglib.Mul( amountA , "997");
	local numerator   = biglib.Mul( amountInWithFee , reserveB);
	local denominator = biglib.Add( biglib.Mul( reserveA , "1000" ) , amountInWithFee);
	local _amount     = biglib.Div( numerator , denominator);
	
	return _amount;
end

function swapTokensForTokens(amountAIn,amountBIn,amountAOutMin,amountBOutMin,deadline)
	--lualib.Assert( biglib.Less(amountIn,"0",true) and biglib.Less(amount0Out,"0",true) , "INSUFFICIENT_LIQUIDITY_BURNED" )

	if biglib.Greater(amountAIn,"0") then
		lualib.TransferToken(Storages.tokenA,sender,addressThis,amountAIn);
		local amountBOut = _getAmountB(amountAIn,Storages.reserveA,Storages.reserveB);
		lualib.Assert( biglib.Greater(amountBOut,"0",true) , "SatswapPair: INSUFFICIENT_OUTPUT_AMOUNT" );
		_swap(amountAIn,"0","0",amountBOut);

	elseif biglib.Greater(amountBIn,"0") then
		lualib.TransferToken(Storages.tokenB,sender,addressThis,amountBIn);
		local amountAOut = _getAmountA(amountBIn,Storages.reserveA,Storages.reserveB);
		lualib.Assert( biglib.Greater(amountAOut,"0",true) , "SatswapPair: INSUFFICIENT_OUTPUT_AMOUNT" );

		_swap("0",amountBIn,amountAOut,"0");
	else
		lualib.Assert( true , "INSUFFICIENT_INPUT_AMOUNT" );

	end


end


function _swap(amountAIn,amountBIn,amountAOut,amountBOut)
	lualib.Assert( biglib.Greater(amountAIn,"0") or biglib.Greater(amountBIn,"0") , "SatswapPair: INSUFFICIENT_OUTPUT_AMOUNT" )
	lualib.Assert( biglib.Less(amountAOut,Storages.reserveA) and biglib.Less(amountBOut,Storages.reserveB) , "SatswapPair: INSUFFICIENT_LIQUIDITY" )
	lualib.Assert( Storages.tokenA ~= sender and Storages.tokenB ~= sender , "SatswapPair: INVALID_TO" )

	local reserveA = Storages.reserveA;
	local reserveB = Storages.reserveB;

	--print("_swap:" .. amountAIn .. " " .. amountBIn .. " " .. amountAOut .. " " .. amountBOut)

	if biglib.Greater(amountAOut,"0") then
		lualib.TransferToken(Storages.tokenA,addressThis,sender,amountAOut);
	elseif biglib.Greater(amountBOut,"0") then
		lualib.TransferToken(Storages.tokenB,addressThis,sender,amountBOut);
	end

    local balanceA = lualib.BalanceOf(Storages.tokenA,addressThis);
    local balanceB = lualib.BalanceOf(Storages.tokenB,addressThis);

	_update(balanceA,balanceB,reserveA,reserveB);

end


function _update(balanceA, balanceB, _reserveA, _reserveB)

	 lualib.Assert( biglib.Greater(balanceA,"0") and biglib.Greater(balanceB,"0") , "SatswapPair: OVERFLOW" )

	 Storages.reserveA = balanceA;
	 Storages.reserveB = balanceB;



end







