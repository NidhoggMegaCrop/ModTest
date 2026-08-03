extends SpineSprite

var spine_anim: SpineAnimationState = null
enum TouchState{IDLE, PRESSED}
var current_state: TouchState = TouchState.IDLE
var touch_box: Control = null
var mouse_inside: bool = false


var is_dragging: bool = false
var spine_bone_node: SpineBoneNode = null
var original_bone_position: Vector2
var original_bone_rotation: float
var last_mouse_position: Vector2
var current_velocity: Vector2
const ROTATION_FACTOR: float = 0.05
const VELOCITY_SMOOTHING: float = 0.3
const MAX_ROTATION: float = deg_to_rad(45)

func _ready() -> void :
	spine_anim = get_animation_state()
	spine_anim.set_animation("attack", true, 0)


	touch_box = $Touch_Box


	spine_bone_node = get_node_or_null("SpineBoneNode")
	if spine_bone_node:
		original_bone_position = spine_bone_node.position
		original_bone_rotation = spine_bone_node.rotation

func _process(delta: float) -> void :
	if not is_dragging or not spine_bone_node:
		return


	var current_mouse_pos = get_global_mouse_position()


	var mouse_delta = current_mouse_pos - last_mouse_position
	current_velocity = lerp(current_velocity, mouse_delta / delta, VELOCITY_SMOOTHING)


	spine_bone_node.global_position = current_mouse_pos


	var horizontal_velocity = current_velocity.x
	var target_rotation = clamp(horizontal_velocity * ROTATION_FACTOR, - MAX_ROTATION, MAX_ROTATION)
	spine_bone_node.rotation = lerp(spine_bone_node.rotation, target_rotation, 0.3)

	last_mouse_position = current_mouse_pos

func _input(event: InputEvent) -> void :
	if event is InputEventMouseMotion:
		_update_mouse_inside(event.global_position)

	if event is InputEventMouseButton and event.button_index == MOUSE_BUTTON_LEFT:
		if event.pressed:
			_try_start_drag(event.global_position)
		elif not event.pressed:
			_try_end_drag()

func _update_mouse_inside(global_pos: Vector2) -> void :
	if not touch_box:
		mouse_inside = false
		return


	var box_global_rect = _get_touch_box_global_rect()
	mouse_inside = box_global_rect.has_point(global_pos)

func _get_touch_box_global_rect() -> Rect2:
	if not touch_box:
		return Rect2()


	return touch_box.get_global_rect()




func _try_start_drag(global_pos: Vector2) -> void :
	if current_state != TouchState.IDLE:
		return


	var box_global_rect = _get_touch_box_global_rect()
	if not box_global_rect.has_point(global_pos):
		return

	current_state = TouchState.PRESSED
	is_dragging = true


	spine_anim.set_animation("idle_loop", true, 0)
	z_index = 1

	last_mouse_position = global_pos
	current_velocity = Vector2.ZERO


	if spine_bone_node:
		spine_bone_node.global_position = global_pos
		spine_bone_node.rotation = 0.0

func _try_end_drag() -> void :
	if current_state != TouchState.PRESSED:
		return

	current_state = TouchState.IDLE
	is_dragging = false


	spine_anim.set_animation("attack", true, 0)
	z_index = 0

	if spine_bone_node:
		var tween = create_tween()
		tween.set_ease(Tween.EASE_OUT)
		tween.set_trans(Tween.TRANS_CUBIC)
		tween.tween_property(spine_bone_node, "position", original_bone_position, 0.3)
		tween.parallel().tween_property(spine_bone_node, "rotation", original_bone_rotation, 0.3)

func _exit_tree() -> void :
	if is_dragging:
		_try_end_drag()
