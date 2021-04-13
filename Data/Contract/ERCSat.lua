
Storages = {}

function create(_name,_symbol,_totalSupply)
	if Storages.publisher==nil then
		Storages.publisher = sender;
		Storages.name    = _name;
		Storages.symbol  = _symbol;
		Storages.decimals  = 8;

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

--返回某个地址(账户)的账户余额
function balanceOf(_owner)
	return lualib.GetAmount(_owner);
end

--从代币合约的调用者地址上转移 _value的数量token到的地址 _to
function transfer(_to, _value)
	lualib.Assert( false , "ERCSat:transfer assert");
end

