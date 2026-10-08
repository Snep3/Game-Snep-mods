-- Snep Highlight
-- One keybind toggles highlights on Gage packages (green) and mission items
-- like keycards, tools and chemicals (magenta).
-- Outlinable models get the game's contour outline; models without outline
-- materials get a small on-screen dot instead.

if not SnepHighlight then
	SnepHighlight = {
		mod_path = ModPath,
		keybind_id = "snep_highlight_toggle",
		all_enabled = false,
		scan_interval = 1,
		next_scan = 0,
	}

	local SH = SnepHighlight

	SH.GAGE_COLOR = Vector3(0, 1, 0)  -- neon green
	SH.ALL_COLOR = Vector3(1, 0, 1)   -- hot magenta

	local ids_contour_color = Idstring("contour_color")
	local ids_contour_opacity = Idstring("contour_opacity")

	-- Mission-item pickups (tools, keycards, chemicals...), as a backup for
	-- interactions that don't declare a special_equipment_block.
	-- Loot (money, jewelry, bags) is deliberately not here.
	local MISSION_ITEMS = {
		gen_pku_crowbar = true,
		pickup_keycard = true,
		pickup_hotel_room_keycard = true,
		lrm_keycard = true,
		pickup_case = true,
		pickup_keys = true,
		hold_take_mask = true,
		pickup_boards = true,
		stash_planks_pickup = true,
		muriatic_acid = true,
		hydrogen_chloride = true,
		caustic_soda = true,
		gen_pku_blow_torch = true,
		drk_pku_blow_torch = true,
		hold_born_receive_item_blow_torch = true,
		gen_pku_thermite = true,
		gen_pku_thermite_paste = true,
		gen_pku_thermite_paste_z_axis = true,
		hold_take_gas_can = true,
		press_pick_up = true,
		hold_take_missing_animal_poster = true,
		hold_pick_up_turtle = true,
		glc_hold_take_handcuffs = true,
		pickup_tablet = true,
		pickup_phone = true,
		press_take_folder = true,
		take_jfr_briefcase = true,
		take_confidential_folder_icc = true,
		mex_pickup_murky_uniforms = true,
		ranc_hold_take_stock = true,
		ranc_hold_take_receiver = true,
		ranc_hold_take_barrel = true,
		pda9_collective_1 = true,
		pda9_collective_2 = true,
		pda9_collective_3 = true,
		pda9_collective_4 = true,
		trai_usb_key = true,
		corp_key_fob = true,
		corp_achi_blueprint = true,
		hold_take_vault_blueprint = true,
		pku_scubagear_vest = true,
		pku_scubagear_tank = true,
	}

	local function is_gage(interaction)
		return interaction.tweak_data == "gage_assignment"
	end

	-- Items that help with the mission. Interactions that hand you special
	-- equipment declare it as special_equipment_block (you can't pick up a
	-- second one while carrying it).
	local function is_mission_item(unit, interaction)
		if unit:carry_data() then
			return false -- loot bags
		end
		local tweak = interaction._tweak_data
		if tweak and tweak.special_equipment_block then
			return true
		end
		return MISSION_ITEMS[tostring(interaction.tweak_data)] or false
	end

	-- The game's list of this unit's outline-capable materials. Models built
	-- without the outline shader (Gage packages, some small loot) have none.
	local function materials_of(interaction)
		return interaction._materials or {}
	end

	local function write_contour(interaction, color, opacity)
		for _, m in ipairs(materials_of(interaction)) do
			m:set_variable(ids_contour_color, color)
			m:set_variable(ids_contour_opacity, opacity)
		end
	end

	-- Fallback for models without outline materials: the unit's contour
	-- extension (the system used for marked enemies / highlighted units).
	local CONTOUR_TYPE = "highlight"

	local function contour_ext(unit)
		local ok, contour = pcall(function() return unit:contour() end)
		return ok and contour or nil
	end

	-- "material" | "contour" | "none", decided once per unit.
	local function outline_method(unit, interaction)
		if not interaction._snep_method then
			if #materials_of(interaction) > 0 then
				interaction._snep_method = "material"
			elseif contour_ext(unit) then
				interaction._snep_method = "contour"
			else
				interaction._snep_method = "none"
			end
		end
		return interaction._snep_method
	end

	---------------------------------------------------------------------------
	-- Dots: for units that can't be outlined (Gage packages, some small loot)
	-- a small colored square is drawn over them on screen, through walls.

	SH.DOT_SIZE = 6
	-- Alpha fades from FAR to NEAR as you approach (distances in cm).
	SH.DOT_ALPHA_FAR = 0.2
	SH.DOT_ALPHA_NEAR = 0.7
	SH.DOT_DIST_FAR = 3000
	SH.DOT_DIST_NEAR = 300
	SH.dots = {}

	local function dot_panel()
		if not SH._dot_ws then
			SH._dot_ws = managers.gui_data:create_fullscreen_workspace()
		end
		return SH._dot_ws:panel()
	end

	local function remove_dot(key)
		local dot = SH.dots[key]
		if dot then
			SH.dots[key] = nil
			if alive(dot.rect) then
				dot.rect:parent():remove(dot.rect)
			end
		end
	end

	local function set_dot(unit, interaction, style)
		local key = unit:key()
		remove_dot(key)
		if not style then
			return
		end
		local rect = dot_panel():rect({
			w = SH.DOT_SIZE,
			h = SH.DOT_SIZE,
			color = style.color,
			alpha = SH.DOT_ALPHA_FAR,
			layer = 100,
			visible = false,
		})
		local position = interaction.interact_position and interaction:interact_position() or unit:position()
		SH.dots[key] = { unit = unit, rect = rect, position = mvector3.copy(position) }
	end

	-- Every frame: move each dot to its unit's spot on screen.
	function SH:update_dots()
		if not next(self.dots) or not self._dot_ws then
			return
		end
		local camera = managers.viewport and managers.viewport:get_current_camera()
		local cam_pos = camera and camera:position()
		local range = self.DOT_DIST_FAR - self.DOT_DIST_NEAR
		for key, dot in pairs(self.dots) do
			if not alive(dot.unit) or not alive(dot.rect) then
				remove_dot(key)
			elseif camera then
				local screen = self._dot_ws:world_to_screen(camera, dot.position)
				local on_screen = screen.z > 0
				dot.rect:set_visible(on_screen)
				if on_screen then
					dot.rect:set_center(screen.x, screen.y)
					local dist = mvector3.distance(cam_pos, dot.position)
					local closeness = math.clamp((self.DOT_DIST_FAR - dist) / range, 0, 1)
					dot.rect:set_alpha(math.lerp(self.DOT_ALPHA_FAR, self.DOT_ALPHA_NEAR, closeness))
				end
			end
		end
	end

	---------------------------------------------------------------------------

	SH.STYLES = {
		gage = { contour = SH.GAGE_COLOR, color = Color(0, 1, 0) },
		all = { contour = SH.ALL_COLOR, color = Color(1, 0, 1) },
	}

	-- Re-applies our outline on top of whatever the game just set.
	function SH:apply(interaction)
		local style = self.STYLES[interaction._snep_hl]
		if style and interaction._snep_method == "material" then
			write_contour(interaction, style.contour, 1)
		end
	end

	-- style_name: "gage" | "all" | nil
	local function set_highlight(unit, interaction, style_name)
		if interaction._snep_hl == style_name then
			return
		end
		interaction._snep_hl = style_name
		local style = SH.STYLES[style_name]
		local method = outline_method(unit, interaction)

		if method == "material" then
			if style then
				SH:apply(interaction)
			elseif interaction._tweak_data and interaction._tweak_data.no_contour then
				write_contour(interaction, Vector3(0, 0, 0), 0)
			else
				-- Hand the outline back to the game.
				interaction:set_contour(interaction._is_selected and "selected_color" or "standard_color")
			end
		elseif method == "contour" then
			local contour = contour_ext(unit)
			if interaction._snep_contour_on then
				pcall(contour.remove, contour, CONTOUR_TYPE, false)
				interaction._snep_contour_on = nil
			end
			if style then
				if pcall(contour.add, contour, CONTOUR_TYPE, false, nil, style.contour) then
					interaction._snep_contour_on = true
				else
					interaction._snep_method = "none"
					set_dot(unit, interaction, style)
				end
			end
		else
			set_dot(unit, interaction, style)
		end
	end

	-- Which style (if any) this unit should have right now.
	function SH:wanted_style(unit, interaction)
		if not self.all_enabled or not interaction:active() then
			return nil
		end
		if is_gage(interaction) then
			return "gage"
		end
		if is_mission_item(unit, interaction) then
			return "all"
		end
		return nil
	end

	function SH:refresh()
		local units = managers.interaction and managers.interaction._interactive_units
		if not units then
			return
		end

		local present = {}
		for _, unit in pairs(units) do
			local interaction = alive(unit) and unit:interaction()
			if interaction then
				present[unit:key()] = true
				local ok, err = pcall(set_highlight, unit, interaction, self:wanted_style(unit, interaction))
				if not ok then
					log("[SnepHighlight] " .. tostring(err))
				end
			end
		end

		-- Drop dots of items that were picked up or deactivated.
		for key in pairs(self.dots) do
			if not present[key] then
				remove_dot(key)
			end
		end
	end

	function SH:toggle_all()
		if not Utils:IsInHeist() then
			return
		end
		self.all_enabled = not self.all_enabled
		self:refresh()
		if managers.hud and managers.hud.show_hint then
			managers.hud:show_hint({ text = "Highlight " .. (self.all_enabled and "ON" or "OFF"), time = 2 })
		end
	end

	-- Periodic scan picks up items that spawn later (dropped bags, keycards...).
	Hooks:Add("GameSetupUpdate", "SnepHighlight_Update", function(t, dt)
		if t >= SH.next_scan then
			SH.next_scan = t + SH.scan_interval
			SH:refresh()
		end
		local ok, err = pcall(SH.update_dots, SH)
		if not ok and not SH._dot_error_logged then
			SH._dot_error_logged = true
			log("[SnepHighlight] dots: " .. tostring(err))
		end
	end)

	---------------------------------------------------------------------------
	-- Options menu (Options > Mod Options > Snep Highlight)

	local MENU_ID = "snep_highlight_menu"

	function MenuCallbackHandler:snep_highlight_back()
		-- Persists the keybind chosen in our menu.
		MenuCallbackHandler:perform_blt_save()
	end

	Hooks:Add("MenuManagerSetupCustomMenus", "SnepHighlight_Setup", function(menu_manager, nodes)
		LocalizationManager:add_localized_strings({
			snep_highlight_menu_title = "Snep Highlight",
			snep_highlight_menu_desc = "Gage package and mission item highlight settings.",
			snep_highlight_key_title = "Highlight toggle key",
			snep_highlight_key_desc = "Press in a heist to toggle highlights: Gage packages in green, mission items (keycards, tools, chemicals...) in magenta.",
		})
		MenuHelper:NewMenu(MENU_ID)
	end)

	Hooks:Add("MenuManagerPopulateCustomMenus", "SnepHighlight_Populate", function(menu_manager, nodes)
		local bind = BLT.Keybinds:get_keybind(SH.keybind_id)
		MenuHelper:AddKeybinding({
			id = "snep_highlight_key",
			title = "snep_highlight_key_title",
			desc = "snep_highlight_key_desc",
			connection_name = SH.keybind_id,
			binding = bind and bind:Key() or "",
			button = SH.keybind_id,
			menu_id = MENU_ID,
			priority = 1,
		})
	end)

	Hooks:Add("MenuManagerBuildCustomMenus", "SnepHighlight_Build", function(menu_manager, nodes)
		nodes[MENU_ID] = MenuHelper:BuildMenu(MENU_ID, { back_callback = "snep_highlight_back" })
		MenuHelper:AddMenuItem(nodes.blt_options, MENU_ID, "snep_highlight_menu_title", "snep_highlight_menu_desc")
	end)
end

-- Keep our outline when the game resets it (looking at / away from items).
-- While you look straight at an item, the game's own "selected" outline wins.
if RequiredScript == "lib/units/interactions/interactionext" and not SnepHighlight.contour_hooked then
	SnepHighlight.contour_hooked = true
	Hooks:PostHook(BaseInteractionExt, "set_contour", "SnepHighlight_SetContour", function(self, color)
		if self._snep_hl and color ~= "selected_color" then
			SnepHighlight:apply(self)
		end
	end)
end
