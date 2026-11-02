extends Control
# Godot 4: Mobile standalone, independent of the PC Unity game.
const PEOPLE = [
 {"name":"마리히","file":"CHR_MARIHI.png"},
 {"name":"찌로","file":"CHR_JIRO.png"},
 {"name":"나즈","file":"CHR_NAZ.png"},
 {"name":"마또","file":"CHR_MATTO.png"},
 {"name":"보키","file":"CHR_BOKI.png"},
 {"name":"문이레","file":"CHR_MUNIRE.png"}
]
const ROOMS = ["현관 홀","응접실","서재","식당","주방","침실","음악실","온실","지하 와인창고"]
const ITEMS = ["황동 촛대","장식용 단검","벽난로 부지깽이","은제 편지칼","오래된 시계추","장미 조각상","무거운 책","유리 화병","철제 열쇠고리"]
const STORAGE = "user://black_rose_mobile_save.json"
var answer = {"suspect":0,"room":0,"item":0}
var excluded = {"suspect":[],"room":[],"item":[]}
var guesses = {"suspect":-1,"room":-1,"item":-1}
var notes: Array[String] = []
var used := 0
var mistakes := 0
var room := 0
var active := false
var ended := false
var won := false
var view := "start"
var notice := ""
var ui: VBoxContainer
var rng := RandomNumberGenerator.new()

func _ready() -> void:
 rng.randomize()
 var bg := ColorRect.new()
 bg.color = Color("#120e17")
 bg.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
 add_child(bg)
 var marg := MarginContainer.new()
 marg.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
 for side in ["left","right","top","bottom"]:
  marg.add_theme_constant_override("margin_" + side,20)
 add_child(marg)
 ui = VBoxContainer.new()
 marg.add_child(ui)
 _load()
 _draw_screen()

func _txt(t: String, sz: int = 24, gold: bool = false) -> Label:
 var l := Label.new()
 l.text = t
 l.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
 l.add_theme_font_size_override("font_size",sz)
 l.add_theme_color_override("font_color",Color("#f0d7a0") if gold else Color("#eee3e3"))
 return l

func _btn(t: String, action: Callable, off: bool = false) -> Button:
 var b := Button.new()
 b.text = t
 b.custom_minimum_size = Vector2(0,65)
 b.size_flags_horizontal = Control.SIZE_EXPAND_FILL
 b.disabled = off
 b.add_theme_font_size_override("font_size",22)
 b.pressed.connect(action)
 return b

func _open(v: String) -> void:
 view = v
 notice = ""
 _draw_screen()

func _art(p: Dictionary, height: int = 280) -> Control:
 var image_path := "res://assets/characters/" + p["file"]
 if not ResourceLoader.exists(image_path):
  return _txt(p["name"],30,true)
 var img := TextureRect.new()
 img.texture = load(image_path)
 img.custom_minimum_size = Vector2(0,height)
 img.expand_mode = TextureRect.EXPAND_IGNORE_SIZE
 img.stretch_mode = TextureRect.STRETCH_KEEP_ASPECT_CENTERED
 img.size_flags_horizontal = Control.SIZE_EXPAND_FILL
 return img

func _page(parent: VBoxContainer) -> void:
 match view:
  "start":
   parent.add_child(_txt("한밤중, 검은 장미 저택에서 사건이 발생했다.",28,true))
   parent.add_child(_txt("여섯 용의자, 아홉 장소, 아홉 도구. 단서를 모아 정답을 찾아보세요.",22))
   parent.add_child(_art(PEOPLE[0],360))
   parent.add_child(_txt("조사 12회 · 최종 지목 2회 · 로컬 자동 저장",19))
   parent.add_child(_btn("새 사건 시작",func(): _new_case()))
   if active and not ended:
    parent.add_child(_btn("이어서 하기",func(): _open("map")))
  "map":
   parent.add_child(_txt("저택 지도",30,true))
   parent.add_child(_txt("방 선택은 자유입니다. 조사할 때만 1회를 소모합니다.",19))
   var grid := GridContainer.new()
   grid.columns = 3
   parent.add_child(grid)
   for i in range(ROOMS.size()):
    var n := i
    grid.add_child(_btn(("◆ " if room == i else "") + ROOMS[i],func(): _select_room(n)))
   parent.add_child(_txt("현재 위치: " + ROOMS[room],25,true))
   parent.add_child(_btn("인물 단서 찾기",func(): _investigate("suspect"),used >= 12))
   parent.add_child(_btn("장소 단서 찾기",func(): _investigate("room"),used >= 12))
   parent.add_child(_btn("도구 단서 찾기",func(): _investigate("item"),used >= 12))
   if notice != "":
    parent.add_child(_txt(notice,20,true))
  "people":
   parent.add_child(_txt("용의자 도감",30,true))
   var grid := GridContainer.new()
   grid.columns = 2
   parent.add_child(grid)
   for i in range(PEOPLE.size()):
    var card := VBoxContainer.new()
    grid.add_child(card)
    card.add_child(_art(PEOPLE[i],220))
    card.add_child(_txt(PEOPLE[i]["name"] + (" (제외)" if excluded["suspect"].has(i) else ""),21,true))
  "notes":
   parent.add_child(_txt("추리 수첩",30,true))
   for kind in ["suspect","room","item"]:
    var heading := "범인 후보" if kind == "suspect" else ("장소 후보" if kind == "room" else "도구 후보")
    parent.add_child(_txt(heading,25,true))
    var names: Array = PEOPLE.map(func(x): return x["name"]) if kind == "suspect" else (ROOMS if kind == "room" else ITEMS)
    for i in range(names.size()):
     parent.add_child(_txt(("✕ " if excluded[kind].has(i) else "◇ ") + names[i],18))
   parent.add_child(_txt("수사 기록",26,true))
   for note in notes:
    parent.add_child(_txt("• " + note,17))
   if ended:
    parent.add_child(_btn("결과 보기",func(): _open("end")))
  "guess":
   parent.add_child(_txt("최종 지목",30,true))
   parent.add_child(_txt("세 항목 모두 일치해야 성공합니다. 틀리면 일치 개수만 확인할 수 있어요.",20))
   _option(parent,"범인","suspect",PEOPLE.map(func(x): return x["name"]))
   _option(parent,"장소","room",ROOMS)
   _option(parent,"도구","item",ITEMS)
   parent.add_child(_btn("이 조합으로 지목하기",func(): _accuse(),used < 3))
   if notice != "":
    parent.add_child(_txt(notice,21,true))
  "end":
   parent.add_child(_txt("사건 해결!" if won else "사건 미해결",33,true))
   parent.add_child(_art(PEOPLE[answer["suspect"]],340))
   parent.add_child(_txt("범인: " + PEOPLE[answer["suspect"]]["name"],27,true))
   parent.add_child(_txt("장소: " + ROOMS[answer["room"]],23))
   parent.add_child(_txt("도구: " + ITEMS[answer["item"]],23))
   parent.add_child(_btn("새 사건",func(): _new_case()))
   parent.add_child(_btn("지난 조사 기록",func(): _open("notes")))

func _option(parent: VBoxContainer, title: String, kind: String, values: Array) -> void:
 parent.add_child(_txt(title,23,true))
 var o := OptionButton.new()
 o.custom_minimum_size = Vector2(0,64)
 o.add_item("선택하세요", -100)
 for i in range(values.size()):
  o.add_item(str(values[i]),i)
 for i in range(o.item_count):
  if o.get_item_id(i) == guesses[kind]:
   o.select(i)
   break
 o.item_selected.connect(func(index: int): guesses[kind] = o.get_item_id(index); _save())
 parent.add_child(o)

func _draw_screen() -> void:
 for c in ui.get_children():
  ui.remove_child(c)
  c.queue_free()
 ui.add_child(_txt("✦ 검은 장미의 비밀",33,true))
 if active:
  ui.add_child(_txt("조사 %d/12 · 실패 %d/2 · %s" % [used,mistakes,ROOMS[room]],17))
 var sc := ScrollContainer.new()
 sc.size_flags_vertical = Control.SIZE_EXPAND_FILL
 sc.horizontal_scroll_mode = ScrollContainer.SCROLL_MODE_DISABLED
 ui.add_child(sc)
 var content := VBoxContainer.new()
 content.add_theme_constant_override("separation",12)
 content.size_flags_horizontal = Control.SIZE_EXPAND_FILL
 sc.add_child(content)
 _page(content)
 if active and not ended:
  var nav := HBoxContainer.new()
  ui.add_child(nav)
  nav.add_child(_btn("저택",func(): _open("map")))
  nav.add_child(_btn("인물",func(): _open("people")))
  nav.add_child(_btn("수첩",func(): _open("notes")))
  nav.add_child(_btn("추리",func(): _open("guess"),used < 3))

func _new_case() -> void:
 answer = {"suspect":rng.randi_range(0,5),"room":rng.randi_range(0,8),"item":rng.randi_range(0,8)}
 excluded = {"suspect":[],"room":[],"item":[]}
 guesses = {"suspect":-1,"room":-1,"item":-1}
 notes.clear()
 used = 0
 mistakes = 0
 room = 0
 active = true
 ended = false
 won = false
 view = "map"
 notice = "첫 번째 조사를 시작하세요."
 _save()
 _draw_screen()

func _select_room(i: int) -> void:
 room = i
 notice = ""
 _save()
 _draw_screen()

func _investigate(kind: String) -> void:
 if used >= 12 or ended:
  return
 var limit := 6 if kind == "suspect" else 9
 var candidates: Array[int] = []
 for i in range(limit):
  if i != answer[kind] and not excluded[kind].has(i):
   candidates.append(i)
 if candidates.is_empty():
  notice = "이 분야의 정답 후보는 이미 하나만 남았습니다."
  _draw_screen()
  return
 candidates.shuffle()
 var names: Array = PEOPLE.map(func(x): return x["name"]) if kind == "suspect" else (ROOMS if kind == "room" else ITEMS)
 var removed: Array[String] = []
 for i in range(min(2 if kind == "suspect" else 3,candidates.size())):
  var index := candidates[i]
  excluded[kind].append(index)
  removed.append(names[index])
 used += 1
 var found := ", ".join(removed)
 var tag := "인물" if kind == "suspect" else ("장소" if kind == "room" else "도구")
 notice = "%s 조사: %s 제외" % [tag,found]
 notes.push_front("%d차 / %s / %s" % [used,ROOMS[room],notice])
 _save()
 _draw_screen()

func _accuse() -> void:
 if used < 3 or ended:
  return
 if guesses["suspect"] < 0 or guesses["room"] < 0 or guesses["item"] < 0:
  notice = "범인·장소·도구를 모두 선택하세요."
  _draw_screen()
  return
 var count := int(guesses["suspect"] == answer["suspect"]) + int(guesses["room"] == answer["room"]) + int(guesses["item"] == answer["item"])
 if count == 3:
  ended = true
  won = true
  view = "end"
 else:
  mistakes += 1
  notice = "오답! 세 항목 중 %d개 일치. 남은 지목 %d회" % [count,2-mistakes]
  notes.push_front(notice)
  if mistakes >= 2:
   ended = true
   view = "end"
 _save()
 _draw_screen()

func _save() -> void:
 var f := FileAccess.open(STORAGE,FileAccess.WRITE)
 if f != null:
  f.store_string(JSON.stringify({"answer":answer,"excluded":excluded,"guesses":guesses,"notes":notes,"used":used,"mistakes":mistakes,"room":room,"active":active,"ended":ended,"won":won}))

func _load() -> void:
 if not FileAccess.file_exists(STORAGE):
  return
 var data = JSON.parse_string(FileAccess.get_file_as_string(STORAGE))
 if typeof(data) != TYPE_DICTIONARY:
  return
 answer = data.get("answer",answer)
 excluded = data.get("excluded",excluded)
 guesses = data.get("guesses",guesses)
 notes.assign(data.get("notes",[]))
 used = int(data.get("used",0))
 mistakes = int(data.get("mistakes",0))
 room = int(data.get("room",0))
 active = bool(data.get("active",false))
 ended = bool(data.get("ended",false))
 won = bool(data.get("won",false))
