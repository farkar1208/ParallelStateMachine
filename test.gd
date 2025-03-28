extends Node2D

@export var text_machine_1 : ParallelStateMachine

func _ready() -> void:
	var text_machine_2 = ParallelStateMachine.new()
	text_machine_2.AddState("1", 10)
	var state1 = text_machine_2.FindState("1") # cs中的索引器跨语言不能用
	var state2 = text_machine_2.AddState("2", 20)
	state2.ConnectState(0, [state1.id])
	var state3 = text_machine_2.AddState("3", 30)
	state3.ConnectState(3, [state1.id, state2.id])
	ResourceSaver.save(text_machine_2, "user://text_machine.res")
	text_machine_2.SelfPrint()
	print("***** ---------------------------- *****")
	text_machine_1 = ResourceLoader.load("user://text_machine.res")
	text_machine_2.SelfPrint()
	print("***** ---------------------------- *****")
	print("***** ---Parallel State Machine--- *****")
	print("***** -------By Cat's Boost------- *****")
	print("***** ---------------------------- *****")
	pass # Replace with function body.
