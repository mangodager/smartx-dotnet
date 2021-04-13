
Storages = {}

function create(_name,_symbol,_tokenA)
	if Storages.publisher==nil then
		Storages.publisher = sender;
		Storages.name    = _name;
		Storages.symbol  = _symbol;
		Storages.decimals  = 8;
		Storages.totalSupply  = "0";

		Storages.allowed = {}

		Storages.tokenA  = _tokenA;

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

--允许 _spender多次从您的帐户转账，最高达 _value金额。 如果再次调用此函数，它将以 _value覆盖当前的余量
function approve(_spender, _value, _lockTime)

	lualib.TransferToken(Storages.tokenA,sender,addressThis,_value);

	if Storages.allowed[sender] == nil then
		Storages.allowed[sender] = {}
	end

	if Storages.allowed[sender][_spender] == nil then
		Storages.allowed[sender][_spender] = {}
	end

	local newAllowed = {};
	newAllowed.Amount = _value;
	newAllowed.LockHeight = curHeight + _lockTime;

	table.insert(Storages.allowed[sender][_spender],newAllowed)
	
    lualib.TransferEvent(sender, _spender, _value);
    return true;
end

--
function retrieved(_from, _to, _lockHeight)
	if Storages.allowed[_from] == nil then
		return false;
	end
	if Storages.allowed[_from][_to] == nil then
		return false;
	end

	local Allowed = Storages.allowed[_from][_to];
	for i=1,#Allowed do
		if Allowed[i].LockHeight == _lockHeight and Allowed[i].LockHeight < curHeight then
			lualib.TransferToken(Storages.tokenA, addressThis, _to, Allowed[i].Amount);
			table.remove(Storages.allowed[_from][_to], i)
			break;
		end
	end

    return true;
end

--返回 _spender仍然被允许从 _owner提取的金额
function allowance(_owner, _spender)
	
	if Storages.allowed[_owner] == nil then
		return false;
	end
	if Storages.allowed[_owner][_spender] == nil then
		return false;
	end

	if #Storages.allowed[_owner][_spender] == 0 then
		return false;
	end

	return rapidjson.encode(Storages.allowed[_owner][_spender]);
end

