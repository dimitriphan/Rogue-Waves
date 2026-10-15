| Scope Files |
	# The Rogue Waves scope, one file per category.
	# Edit a file, then send back only the files you changed. Claude diffs each against its copy, keeps your wording, and updates both copies.
	# Every file follows the rules below. A ! NEEDS ! line is waiting on you, and your < Answer > goes right under it.
	/~ The repository copy is docs/scope/ and the project files copy is scope/. Both are kept the same \

	[Files]
		- 00_Read_Me.txt: this list and the rules
		- 01_Expansion.txt: what the pack is, each part pointing to its file
		- 02_Progress.txt: where the work stands, and the log of every change. Claude keeps this one current
		- 03_Bug_Fix.txt: the bugs the pack fixes, in the Bug template
		- 04_Rogue_Waves_Mode.txt: the mode's rules: campaign, retries, difficulty, Depth, tides and leaving, the sea chart and sea weather
		- 05_Gameplay_Rundown.txt: a campaign step by step, from the main menu to the last map
		- 06_New_Mechanics.txt: [Directions], [Pure Aura], [Fund Transfer], [Shopkeeper]
		- 07_Vessel_Yossele.txt, 08_Vessel_Spool.txt, 09_Vessel_Argo.txt, 10_Vessel_Uloe.txt: one vessel each, in the Vessel template
		- 11_Directions.txt: every vessel's Directions
		- 12_Cardinals.txt: the Cardinal rules and the Cardinal List
		- 13_Enemy_Modifiers.txt: Cursed, Berserk, Aging, Dazzling
		- 14_Optional_Objectives.txt: the objective rules and the Optional Objective List
		- 15_Templates.txt: every template: Vessel, Direction, Cardinal, Optional Objective, Enemy Modifier, Sea Weather, Bug
		- 16_Base_Vessels.txt: Smacky, Sno Sno, Boutman, Fie and Buster in the Vessel template with the game's own numbers, and every vessel's stats side by side

| Rules |
	[Symbols] / One meaning each, and no symbol is used for anything else \
		- | Title |: the top of a file, or the top of one sheet in a file of sheets / | Vessel: Yossele |, | Base Vessel: Smacky |, | Directions | \
		- [Name]: a game system or a sheet's section / [Pure Aura], [Growth], [Stats], [Upgrades] \
		- {Name}: one named thing: a vessel, building, unit, ability, resource, Direction, Cardinal, objective, modifier, weather / {Loom}, {VENDCINT}, {Thread} \
		- (Kind): a category or a slot that a thing fills / (Offense 1), (Minion), (Any), (Team Cardinals) \
		- % Level:{Name}: an upgrade, its level first / % 2:{Natural Remedy} \
		- : connector: joins a slot to its value, or a kind to its name / - Cost: Red 30, (Offense 1):{Scatter Cannon} \
		- # line: a design note, the why or the feel. It never holds a value Claude builds from \
		- - line: something to build. With a colon it is a slot: - Field: value. Without one it is a detail of the line above it \
		- / note \: extra info at the end of a line, one per line \
		- /~ note \: a note Claude wrote, so yours and Claude's never mix \
		- ~value: a default Claude picked. Delete the ~ to make it yours. Works on names too: {~Sharper Pins} \
		- ! NEEDS ... !: something only you can supply. It sits in the slot it fills: - Cost: ! NEEDS ! \
		- < answer >: your answer, under a NEEDS line or a question. Claude folds it in and deletes both \
		- ->: points to another file or sheet / -> 11_Directions.txt \

	[Values]
		- Every number sits on a - Field: value line with its unit. A sentence may repeat it, never hold it alone
		- Units: distance in tiles, time in seconds, rates per second, a change in % (+20%) and a multiplier with x (x1.25)
		- Money: the colour or currency, then the amount / Red 30, Blue 20 + Yellow 10, Pure Aura 20 \
		- A blank slot means not decided yet, and the design gate proposes a ~ value. ! NEEDS ! means Claude can't pick it for you
		- A value Claude read from the game (the base vessels) carries no ~: it is the game's, not a default

	[Names]
		- Title Case inside { } and [ ]. Base game names are spelled as the game spells them / Sno Sno, Boutman, Bouthouse \
		- A name is singular inside the braces. Plurals and possessives go outside / {Photon}s, {Loom}'s range \
		- A thing has one name everywhere. A renamed thing is renamed in every file at once
		- Name styles stay as docs/STYLE.md sets them: a Direction like a heading or a course, a Cardinal two plain words for a found object, an objective a short order

	[Layout]
		- Tabs indent, one tab per level: | Title | at 0, sections and sheets at 1, slots at 2, details of a slot at 3
		- One fact per line. A paragraph of prose is allowed only as a file's opening [Summary], and every rule in it is also a slot below
		- Every file follows the same order: | Title |, # lines saying what the file holds, its rules, then its list of sheets, then [Questions]
		- Every sheet in a list follows its template from 15_Templates.txt, in the template's slot order
		- Slots every template shares sit in the same places: Kind first, Cost or Pays just before Look, Look and Clashes last, Status at the very end

	[One Place for Each Fact]
		- A rule lives in one file. Any other file points to it with -> instead of repeating it
		- History and dates live only in 02_Progress.txt. Other files say what is true now, with no dates and no "picked on" or "adopted on" notes
		- A sheet may carry one - Status: line: where it stands now, no date. Claude keeps it current
		- Claude's references to repo documents (docs/..., PR numbers) go only in /~ \ notes or in 02
		- Examples ("Example only, not a decision") live only in 15_Templates.txt

	[Questions and Needs]
		- ! NEEDS ! sits in the slot it fills. A NEEDS that belongs to no slot sits at the top of its file
		- Claude's questions go in a [Questions] block at the end of the sheet or file they concern, each naming the default in use
		- A NEEDS line never carries a / \ note with defaults in it. The defaults go in their own slots as ~ values

	[File Names]
		- Named NN_Name.txt, numbered as in the list above. Claude keeps that list current
		- You send back only the files you changed. Claude diffs each against its copy, keeps your wording, and updates both copies
