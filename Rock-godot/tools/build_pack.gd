extends SceneTree

const PACK_ENTRIES := [
	"res://project.godot",
	"res://mod_manifest.json",
	"res://icon.svg",
	"res://icon.svg.import",
	"res://.godot/imported/icon.svg-218a8f2b3041327d8a5756f3a245f83b.ctex",
	"res://.godot/imported/icon.svg-218a8f2b3041327d8a5756f3a245f83b.md5",
	"res://Assets/RpsIcons/rock.png",
	"res://Assets/RpsIcons/rock.png.import",
	"res://.godot/imported/rock.png-aca8e78867a83a48651701498abc0f97.ctex",
	"res://.godot/imported/rock.png-aca8e78867a83a48651701498abc0f97.md5",
	"res://Assets/RpsIcons/paper.png",
	"res://Assets/RpsIcons/paper.png.import",
	"res://.godot/imported/paper.png-2ed78b5875483a8c091386f93f0bc7d3.ctex",
	"res://.godot/imported/paper.png-2ed78b5875483a8c091386f93f0bc7d3.md5",
	"res://Assets/RpsIcons/scissors.png",
	"res://Assets/RpsIcons/scissors.png.import",
	"res://.godot/imported/scissors.png-620d4c541f940e4ecc5772e89d395c91.ctex",
	"res://.godot/imported/scissors.png-620d4c541f940e4ecc5772e89d395c91.md5",
]

func _init() -> void:
	var output_dir := ProjectSettings.globalize_path("res://build")
	DirAccess.make_dir_recursive_absolute(output_dir)

	var output_path := ProjectSettings.globalize_path("res://build/Rock.pck")
	var packer := PCKPacker.new()
	var start_error := packer.pck_start(output_path)
	if start_error != OK:
		push_error("pck_start failed: %s" % start_error)
		quit(1)
		return

	for res_path in PACK_ENTRIES:
		var source_path := ProjectSettings.globalize_path(res_path)
		if not FileAccess.file_exists(source_path):
			push_error("missing pack source: %s -> %s" % [res_path, source_path])
			quit(2)
			return

		var add_error := packer.add_file(res_path, source_path)
		if add_error != OK:
			push_error("add_file failed: %s (%s)" % [res_path, add_error])
			quit(3)
			return

		print("PACK ", res_path)

	var flush_error := packer.flush(true)
	if flush_error != OK:
		push_error("flush failed: %s" % flush_error)
		quit(4)
		return

	print("PACK_OK ", output_path)
	quit()
