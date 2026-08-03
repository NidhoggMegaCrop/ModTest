extends SpineBoneNode

@export_group("Position Settings")
@export var max_position_offset: float = 1.0

@export_group("Rotation Settings")
@export var max_rotation_angle: float = 2.0

@export_group("Advanced")
@export var invert_x: bool = false
@export var invert_y: bool = false
@export var invert_rotation: bool = false

@export var debounce_time: float = 0.5
@export var return_speed: float = 5.0

var initial_position: Vector2
var initial_rotation: float
var screen_size: Vector2
var is_dragging: bool = false
var is_returning: bool = false
var touch_rect: Rect2
var mouse_inside: bool = false
var last_drag_end_time: float = 0.0
var target_position: Vector2
var target_rotation: float

func _ready():
	initial_position = position
	initial_rotation = rotation_degrees
	target_position = initial_position
	target_rotation = initial_rotation
	screen_size = get_viewport().get_visible_rect().size


	bone_mode = SpineConstant.BoneMode_Follow


	call_deferred("_setup_touch_rect")

func _setup_touch_rect():

	var parent_node = get_parent()
	if not parent_node:
		print("No parent node found!")
		return

	var touch_box = parent_node.get_node_or_null("Touch_Box_Head")

	if touch_box:

		await get_tree().process_frame
		var box_rect = touch_box.get_rect()
		touch_rect = Rect2(touch_box.global_position, box_rect.size)
	else:
		print("Touch_Box_Head not found! Available children in parent (", parent_node.name, "):")
		for child in parent_node.get_children():
			print("  - " + child.name + " [" + child.get_class() + "]")

func _input(event):

	if event is InputEventMouseMotion:
		_update_mouse_inside(event.global_position)
		return


	if event is InputEventMouseButton and event.button_index == MOUSE_BUTTON_LEFT:
		if event.pressed:
			_update_mouse_inside(event.global_position)
			if mouse_inside and _can_start_drag():
				bone_mode = SpineConstant.BoneMode_Drive
				is_dragging = true
				is_returning = false
		else:
			if is_dragging:
				is_dragging = false
				bone_mode = SpineConstant.BoneMode_Follow
				last_drag_end_time = Time.get_ticks_msec() / 1000.0
				_start_return_animation()
		return

func _can_start_drag() -> bool:

	var current_time = Time.get_ticks_msec() / 1000.0
	return (current_time - last_drag_end_time) >= debounce_time

func _start_return_animation():

	is_returning = true
	target_position = initial_position
	target_rotation = initial_rotation

func _update_mouse_inside(pos: Vector2) -> void :
	var was_inside = mouse_inside
	mouse_inside = touch_rect.has_point(pos)


	if was_inside and not mouse_inside and is_dragging:
		is_dragging = false
		bone_mode = SpineConstant.BoneMode_Follow
		last_drag_end_time = Time.get_ticks_msec() / 1000.0
		_start_return_animation()

func _process(_delta):
	if is_dragging and mouse_inside:

		var mouse_pos = get_viewport().get_mouse_position()

		var normalized_mouse = Vector2(
			(mouse_pos.x / screen_size.x - 0.5) * 2.0, 
			(mouse_pos.y / screen_size.y - 0.5) * 2.0
		)

		var pos_x = normalized_mouse.x * (-1.0 if invert_x else 1.0)
		var pos_y = normalized_mouse.y * (-1.0 if invert_y else 1.0)
		var rot_dir = -1.0 if invert_rotation else 1.0

		target_position = initial_position + Vector2(pos_x * max_position_offset, pos_y * max_position_offset)
		target_rotation = initial_rotation + normalized_mouse.x * max_rotation_angle * rot_dir


		position = target_position
		rotation_degrees = target_rotation

	elif is_returning:

		var delta = get_process_delta_time()


		position = position.lerp(target_position, return_speed * delta)
		rotation_degrees = lerp(rotation_degrees, target_rotation, return_speed * delta)


		if position.distance_to(target_position) < 0.01 and abs(rotation_degrees - target_rotation) < 0.01:
			position = target_position
			rotation_degrees = target_rotation
			is_returning = false
