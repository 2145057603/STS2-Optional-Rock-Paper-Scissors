extends SceneTree

const TEST_PATHS := [
	"res://Assets/RpsIcons/rock.png",
	"res://Assets/RpsIcons/paper.png",
	"res://Assets/RpsIcons/scissors.png",
]

func _init() -> void:
	var pack_path := ProjectSettings.globalize_path("res://build/Rock.pck")
	var loaded := ProjectSettings.load_resource_pack(pack_path, true)
	print("LOAD_PACK ", loaded, " ", pack_path)
	if not loaded:
		quit(1)
		return

	for path in TEST_PATHS:
		var exists := ResourceLoader.exists(path)
		var resource := ResourceLoader.load(path)
		print("CHECK ", path, " exists=", exists, " resource=", resource != null)
		if resource != null and resource is Texture2D:
			print("SIZE ", path, " ", (resource as Texture2D).get_size())

	quit()
