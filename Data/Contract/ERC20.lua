
Storages = {}

function create(_name,_symbol,_totalSupply)
	if Storages.publisher==nil then
		Storages.publisher = sender;
		Storages.name    = _name;
		Storages.symbol  = _symbol;
		Storages.decimals  = 8;
		Storages.totalSupply  = _totalSupply;

		luaDB.SetValue("balances",sender,_totalSupply);

        lualib.TransferEvent(sender, "", "create: " .. addressThis);
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

--允许 _spender多次从您的帐户转账，最高达 _value金额。 如果再次调用此函数，它将以 _value覆盖当前的余量
function approve(_spender, _value)
	local allowed_sender = luaDB.GetValue("allowed",sender);

	if allowed_sender == nil then
		allowed_sender = {}
	end

    allowed_sender[_spender] = _value;
	luaDB.SetValue("allowed",sender,allowed_sender);

    lualib.TransferEvent(sender, _spender, _value);
    return true;
end

--从地址 _from发送数量为 _value的token到地址 _to
function transferFrom(_from, _to, _value)

	local allowed_from = luaDB.GetValue("allowed",_from);
	if allowed_from == nil then
		allowed_from = {}
	end

	lualib.Assert( _from ~= _to , "transfer: _from == _to" );
	lualib.Assert( allowed_from[_to] ~= nil , "transfer: _from == nil" );

	local value_from = luaDB.GetValue("balances",_from);
	local value_to   = luaDB.GetValue("balances",_to);

	if biglib.Greater(value_from,_value,true) and biglib.Greater(allowed_from[_to],_value,true)
			and biglib.Greater( biglib.Add(value_to , _value) , value_to,true) then

        value_from = biglib.Sub(value_from,_value);
        value_to   = biglib.Add(value_to,_value);
        allowed_from[_to]  = biglib.Sub(allowed_from[_to],_value);

		luaDB.SetValue("balances",_from,value_from);
		luaDB.SetValue("balances",_to,value_to);
		luaDB.SetValue("allowed",_from,allowed_from);

        lualib.TransferEvent(sender, _to, _value);
        return true;
	end
	lualib.Assert( false , "transferFrom: error" );
end

--返回 _spender仍然被允许从 _owner提取的金额
function allowance(_from, _spender)
	local allowed_from = luaDB.GetValue("allowed",_from);
	if allowed_from == nil then
		allowed_from = {}
	end

	return allowed_from[_spender]
end

