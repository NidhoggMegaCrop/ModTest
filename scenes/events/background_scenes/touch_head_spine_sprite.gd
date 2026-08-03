extends SpineSprite

var spine_anim: SpineAnimationState = null
enum TouchState{IDLE, BEGIN, LOOP, END}
var current_state: TouchState = TouchState.IDLE
var touch_rect: Rect2
var mouse_inside: bool = false

func _ready() -> void :
	spine_anim = get_animation_state()

	spine_anim.set_animation("loop", true, 0)

	var touch_box = $Touch_Box_Head
	if touch_box:
		var box_rect = touch_box.get_rect()
		touch_rect = Rect2(touch_box.global_position, box_rect.size)

	animation_completed.connect(_on_animation_completed)

func _input(event: InputEvent) -> void :
	if event is InputEventMouseMotion:
		_update_mouse_inside(event.global_position)

	if event is InputEventMouseButton and event.button_index == MOUSE_BUTTON_LEFT:
		if event.pressed and touch_rect.has_point(event.global_position):
			mouse_inside = true
			_try_start_touch()
		elif not event.pressed:
			mouse_inside = false
			_try_end_touch()

func _update_mouse_inside(pos: Vector2) -> void :
	var was_inside = mouse_inside
	mouse_inside = touch_rect.has_point(pos)

	if was_inside and not mouse_inside and current_state in [TouchState.BEGIN, TouchState.LOOP]:
		_try_end_touch()

func _try_start_touch() -> void :
	if current_state != TouchState.IDLE:
		return

	current_state = TouchState.BEGIN

	spine_anim.set_animation("touch_head_begin", false, 0)

func _try_end_touch() -> void :
	if current_state in [TouchState.END, TouchState.IDLE]:
		return

	current_state = TouchState.END
	spine_anim.set_animation("touch_head_end", false, 0)

func _on_animation_completed(sprite: SpineSprite, animation_state: SpineAnimationState, track_entry: SpineTrackEntry) -> void :
	var anim_name = track_entry.get_animation().get_name()

	match anim_name:
		"touch_head_begin":
			if mouse_inside:
				current_state = TouchState.LOOP
				spine_anim.set_animation("touching_head_loop", true, 0)
			else:
				_try_end_touch()

		"touch_head_end":
			current_state = TouchState.IDLE


			spine_anim.clear_track(0)
			spine_anim.set_animation("loop", true, 0)
