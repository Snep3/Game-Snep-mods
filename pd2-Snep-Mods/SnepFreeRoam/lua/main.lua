-- Snep Free Roam
-- Pause menu buttons that delete all guards / civilians without killing them.
-- Units are despawned (slot 0) instead of killed, so no death callbacks run:
-- no pagers, no bodies, no kill counters.

SnepFreeRoam = SnepFreeRoam or {}
local FR = SnepFreeRoam

FR.chat_name = "Free Roam"

local UNIT_NAMES = {
	security = "Security Guard",
	security_undominatable = "Security Guard",
	security_mex = "Security Guard",
	security_mex_no_pager = "Security Guard",
	gensec = "GenSec Guard",
	cop = "Cop",
	cop_female = "Cop",
	cop_scared = "Cop",
	fbi = "FBI Agent",
	fbi_female = "FBI Agent",
	gangster = "Gangster",
	mobster = "Mobster",
	biker = "Biker",
	biker_escape = "Biker",
	triad = "Triad",
	civilian = "Civilian",
	civilian_female = "Civilian",
	civilian_mariachi = "Civilian",
	bank_manager = "Bank Manager",
	drunk_pilot = "Drunk Pilot",
	escort = "Escort",
}

local function pretty(str)
	str = tostring(str):gsub("_", " ")
	return (str:gsub("(%a)(%w*)", function(first, rest) return first:upper() .. rest end))
end

local function unit_display_name(unit)
	local tweak = unit:base() and unit:base()._tweak_table
	return UNIT_NAMES[tweak] or pretty(tweak or "Unknown")
end

-- Local-only chat line (only you see it) + log file entry.
local function chat(msg)
	log("[SnepFreeRoam] " .. msg)
	if managers.chat then
		managers.chat:_receive_message(ChatManager.GAME, FR.chat_name, msg, tweak_data.system_chat_color or Color.green)
	end
end

local function hint(msg)
	if managers.hud and managers.hud.show_hint then
		managers.hud:show_hint({ text = msg, time = 3 })
	end
end

function FR:can_use()
	if not Utils:IsInHeist() then
		return false, "You need to be in a heist."
	end
	-- Only the host owns AI units; a client deleting them would just desync.
	if not Network:is_server() then
		return false, "Host / offline only."
	end
	return true
end

local function is_converted(unit)
	local state = managers.groupai and managers.groupai:state()
	if state and state.is_enemy_converted_to_criminal and state:is_enemy_converted_to_criminal(unit) then
		return true
	end
	local brain = unit:brain()
	return brain and brain._logic_data and brain._logic_data.is_converted or false
end

local function collect(list, skip_converted)
	local units = {}
	for _, data in pairs(list or {}) do
		local unit = data.unit
		local dmg = alive(unit) and unit:character_damage()
		if dmg and not dmg:dead() and not (skip_converted and is_converted(unit)) then
			table.insert(units, unit)
		end
	end
	return units
end

-- Civilians the heist always relies on, by tweak name (anything starting
-- with "escort" is also kept).
local CRITICAL_CIVILIANS = {
	bank_manager = true,
	drunk_pilot = true,
	boris = true,
	spa_vip = true,
	spa_vip_hurt = true,
	old_hoxton_mission = true,
	robbers_safehouse = true,
	captain = true,
}

-- Mission-script events that don't mean progression depends on the unit.
local HARMLESS_EVENTS = {
	spawn = true,
	alerted = true,
	panic = true,
	weapons_hot = true,
	enemy_spotted = true,
}

-- Ordinary bystanders. Heists often attach script events to these too
-- (hostage counts, ambience), so they are never treated as critical.
local GENERIC_CIVILIANS = {
	civilian = true,
	civilian_female = true,
	civilian_mariachi = true,
	civilian_no_penalty = true,
}

-- Returns the first mission-script event that suggests progression depends
-- on this unit (death, tied, intimidated, ...), or nil.
local function mission_event_on(unit)
	local element = unit:unit_data() and unit:unit_data().mission_element
	local events = element and element._events
	if type(events) ~= "table" then
		return nil
	end
	for event, callbacks in pairs(events) do
		if not HARMLESS_EVENTS[event] and not tostring(event):find("^anim_act") and type(callbacks) == "table" and next(callbacks) then
			return tostring(event)
		end
	end
	return nil
end

-- Returns a reason string if the civilian should be kept, else nil.
local function critical_reason(unit)
	local tweak = unit:base() and unit:base()._tweak_table or ""
	if CRITICAL_CIVILIANS[tweak] or tweak:find("^escort") then
		return "type " .. tweak
	end
	if GENERIC_CIVILIANS[tweak] then
		return nil
	end
	local event = mission_event_on(unit)
	return event and string.format("type %s, mission event '%s'", tweak, event) or nil
end

-- Turns off detection on every camera. Returns how many were disabled.
local function disable_cameras()
	local cameras = {}
	for _, unit in pairs(SecurityCamera and SecurityCamera.cameras or {}) do
		cameras[unit:key()] = unit
	end
	local state = managers.groupai and managers.groupai:state()
	for key, unit in pairs(state and state._security_cameras or {}) do
		cameras[key] = unit
	end

	local count = 0
	for _, unit in pairs(cameras) do
		local base = alive(unit) and unit:base()
		if base and base.set_detection_enabled and not base._destroyed then
			local success, err = pcall(base.set_detection_enabled, base, false)
			if success then
				count = count + 1
			else
				log("[SnepFreeRoam] failed to disable camera: " .. tostring(err))
			end
		end
	end
	return count
end

-- Take the pickup (keycard etc.) a unit is carrying, if any.
local function take_pickup(unit, player_pos)
	local dmg = unit:character_damage()
	local pickup = dmg and dmg._pickup
	if not pickup or pickup:find("ammo") or not tweak_data.pickups[pickup] then
		return nil
	end
	dmg._pickup = nil

	return {
		name = pickup,
		from = unit_display_name(unit),
		pos = mvector3.copy(unit:position()),
		dist = player_pos and math.floor(mvector3.distance(player_pos, unit:position()) / 100) or nil,
	}
end

local function despawn(unit)
	if unit:base() and unit:base().set_slot then
		unit:base():set_slot(unit, 0)
	else
		unit:set_slot(0)
	end
end

local function spawn_drops(drops)
	local player = managers.player:player_unit()
	for i, drop in ipairs(drops) do
		local pos = drop.pos
		if alive(player) then
			-- First item right under you, the rest in a small ring so each is interactable.
			local radius = i == 1 and 0 or 50
			local angle = (i - 1) * 60 -- degrees (Diesel math uses degrees)
			pos = player:position() + Vector3(math.cos(angle) * radius, math.sin(angle) * radius, 5)
		end
		managers.game_play_central:spawn_pickup({ name = drop.name, position = pos, rotation = Rotation() })

		local where = drop.dist and string.format(" (was %dm from you)", drop.dist) or ""
		chat(string.format("%s dropped from %s%s", pretty(drop.name), drop.from, where))
	end
end

-- kind: "guards" | "civilians"
function FR:remove(kind)
	local ok, reason = self:can_use()
	if not ok then
		chat(reason)
		hint(reason)
		return
	end

	local units
	if kind == "guards" then
		units = collect(managers.enemy:all_enemies(), true)
	else
		units = collect(managers.enemy:all_civilians(), false)
	end

	local player = managers.player:player_unit()
	local player_pos = alive(player) and player:position() or nil
	local drops, removed, kept = {}, 0, {}

	for _, unit in ipairs(units) do
		local reason = kind == "civilians" and critical_reason(unit)
		if reason then
			-- Keep the unit, but still hand over anything it carries.
			log(string.format("[SnepFreeRoam] keeping %s (%s)", unit_display_name(unit), reason))
			table.insert(kept, unit_display_name(unit))
			table.insert(drops, take_pickup(unit, player_pos))
		else
			local drop = take_pickup(unit, player_pos)
			local success, err = pcall(despawn, unit)
			if success then
				removed = removed + 1
				table.insert(drops, drop)
			else
				log("[SnepFreeRoam] failed to remove unit: " .. tostring(err))
			end
		end
	end

	spawn_drops(drops)

	if #kept > 0 then
		chat(string.format("Kept %d civilian(s) needed for the heist: %s", #kept, table.concat(kept, ", ")))
	end

	local summary = string.format("Removed %d %s, %d item(s) dropped.", removed, kind, #drops)
	if kind == "guards" then
		summary = summary .. string.format(" Disabled %d camera(s).", disable_cameras())
	end
	chat(summary)
	hint(summary)
end

---------------------------------------------------------------------------
-- Pause menu

function MenuCallbackHandler:snep_freeroam_remove_guards()
	FR:remove("guards")
end

function MenuCallbackHandler:snep_freeroam_remove_civilians()
	FR:remove("civilians")
end

local BUTTONS = {
	{
		id = "snep_freeroam_guards",
		text = "Remove All Guards",
		help = "Silently delete every guard/enemy and disable all cameras (no pagers, no alarm). Host/offline only.",
		callback = "snep_freeroam_remove_guards",
	},
	{
		id = "snep_freeroam_civilians",
		text = "Remove All Civilians",
		help = "Silently delete every civilian except ones the heist needs. Host/offline only.",
		callback = "snep_freeroam_remove_civilians",
	},
}

Hooks:Add("MenuManagerBuildCustomMenus", "SnepFreeRoam_PauseButtons", function(menu_manager, nodes)
	local node = nodes.pause
	if not node then
		return
	end

	local strings = {}
	for _, def in ipairs(BUTTONS) do
		strings[def.id .. "_title"] = def.text
		strings[def.id .. "_desc"] = def.help
	end
	LocalizationManager:add_localized_strings(strings)

	-- Place the buttons right after "Resume Game".
	local position = 1
	for i, item in ipairs(node._items or {}) do
		if item:name() == "resume_game" then
			position = i + 1
			break
		end
	end

	for i, def in ipairs(BUTTONS) do
		if not node:item(def.id) then
			local item = node:create_item({ type = "CoreMenuItem.Item" }, {
				name = def.id,
				text_id = def.id .. "_title",
				help_id = def.id .. "_desc",
				callback = def.callback,
			})
			node:insert_item(item, position + i - 1)
		end
	end
end)
