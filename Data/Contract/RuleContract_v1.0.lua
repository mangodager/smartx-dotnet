
Storages = {}
Storages.Rules = {}

function create()
	if Storages.Publisher==nil then
		Storages.Publisher = sender;
		local Rule   = {}
		Rule.Address = sender
		Rule.Start   = curHeight
		Rule.End     = -1
		Rule.LBH     = curHeight
		table.insert(Storages.Rules,Rule)
	end
end

function sort(a,b)
	if a.Address == Storages.Publisher then
		return true
	end
	if b.Address == Storages.Publisher then
		return false
	end
	if biglib.Equals(a.Amount , b.Amount) then
		return lualib.StringCompare(a.Address , b.Address) > 0;
	end

	return biglib.Greater(a.Amount , b.Amount,false);
end

function add()
	if biglib.Less(lualib.GetAmount(sender) , "1000000") then
		return
	end

	-- is kickout
	local find = false
	for i = #Storages.Rules , 1 , -1 do
		if Storages.Rules[i].Address == sender then
			find = true
		end
	end

	-- else add
	if find == false then
		local Rule = {}
		Rule.Address = sender
		Rule.Start   = curHeight + 10
		Rule.End     = -1
		Rule.LBH     = curHeight
		table.insert(Storages.Rules,Rule)
	end

end


function update()

	-- delete height out
	for i = #Storages.Rules , 1 , -1 do
		if Storages.Rules[i].End ~= -1 and Storages.Rules[i].End < curHeight then
			table.remove(Storages.Rules,i)
		end
	end

	-- updata and check amount
	for i=1,#Storages.Rules do
		Storages.Rules[i].Amount = lualib.GetAmount(Storages.Rules[i].Address)
	end

	table.sort(Storages.Rules,sort)

	for i=1,25 do
		if Storages.Rules[i] == nil then
			break;
		end
		if biglib.Less(Storages.Rules[i].Amount , "1000000") and Storages.Rules[i].End == -1 and Storages.Rules[i].Address ~= Storages.Publisher then
			Storages.Rules[i].End = curHeight + 10
		end
		if biglib.Greater(Storages.Rules[i].Amount , "1000000",true) and Storages.Rules[i].End ~= -1 then
			Storages.Rules[i].End = -1
		end
		if lualib.IsRuleOnline(curHeight,Storages.Rules[i].Address) then
			Storages.Rules[i].LBH = curHeight
		end
		if curHeight - Storages.Rules[i].LBH > 120 and Storages.Rules[i].End == -1 and Storages.Rules[i].Address ~= Storages.Publisher then
			Storages.Rules[i].End = curHeight + 10
		end

	end

	-- kickout the last
	for i=26,#Storages.Rules do
		if Storages.Rules[i].End == -1 and Storages.Rules[i].Address ~= Storages.Publisher then
			Storages.Rules[i].End = curHeight + 10
		end
	end


end



