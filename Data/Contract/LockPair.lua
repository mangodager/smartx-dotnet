
Storages = {};
Storages.alloweds = {};

function create(_name,_symbol)
	if Storages.publisher==nil then
		Storages.publisher = sender;
		Storages.name    = _name;
		Storages.symbol  = _symbol;
		Storages.decimals  = 8;
		Storages.totalSupply  = "0";

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
function approve(_tokenA,_from,_to, _value, _lockTime,_name)
	lualib.TransferToken(_tokenA,_from,addressThis,_value);

	local newAllowed = {};
	newAllowed.tokenA = _tokenA;
	newAllowed.Amount = _value;
	newAllowed.To     = _to;
	newAllowed.Name   = _name;
	newAllowed.LockHeight = curHeight + _lockTime;

	if #Storages.alloweds == 0 then
		Storages.alloweds = {};
	end

	table.insert(Storages.alloweds,newAllowed);

    lualib.TransferEvent(sender, _spender, _value);
    return true;
end

--
function retrieved(_to, _lockHeight)

	local Allowed = Storages.alloweds;
	for i=1,#Allowed do
		if Allowed[i].LockHeight == _lockHeight and Allowed[i].LockHeight < curHeight then
			lualib.TransferToken(Allowed[i].tokenA, addressThis, Allowed[i].To, Allowed[i].Amount);
			table.remove(Storages.alloweds, i)
			break;
		end
	end

    return true;
end

--返回 _spender仍然被允许从 _owner提取的金额
function allowance()
	return rapidjson.encode(Storages.alloweds);
end

