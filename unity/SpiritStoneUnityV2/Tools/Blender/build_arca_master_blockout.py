import bpy
import math
from pathlib import Path
from mathutils import Vector


SCRIPT_PATH = Path(__file__).resolve()
PROJECT_ROOT = SCRIPT_PATH.parents[2]
ARCA_ROOT = PROJECT_ROOT / "Assets" / "Characters" / "Arca"
BLENDER_ROOT = ARCA_ROOT / "Blender"
PREVIEW_ROOT = BLENDER_ROOT / "Previews"
BLEND_PATH = BLENDER_ROOT / "Arca_Master.blend"
PREVIEW_PATH = PREVIEW_ROOT / "Arca_Master_DetailV14_Front.png"


def srgb_to_linear(value):
    return value / 12.92 if value <= 0.04045 else ((value + 0.055) / 1.055) ** 2.4


def material(name, color, metallic=0.0, roughness=0.7):
    linear_color = tuple(srgb_to_linear(channel) for channel in color)
    shadow_color = tuple(channel * (0.42 if metallic < 0.4 else 0.55) for channel in linear_color)
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = (*linear_color, 1.0)
    mat.use_nodes = True
    nodes = mat.node_tree.nodes
    links = mat.node_tree.links
    nodes.clear()
    output = nodes.new("ShaderNodeOutputMaterial")
    diffuse = nodes.new("ShaderNodeBsdfDiffuse")
    diffuse.inputs["Color"].default_value = (*linear_color, 1.0)
    diffuse.inputs["Roughness"].default_value = roughness
    shader_to_rgb = nodes.new("ShaderNodeShaderToRGB")
    ramp = nodes.new("ShaderNodeValToRGB")
    ramp.color_ramp.interpolation = "CONSTANT"
    ramp.color_ramp.elements[0].position = 0.46
    ramp.color_ramp.elements[0].color = (*shadow_color, 1.0)
    ramp.color_ramp.elements[1].position = 0.54
    ramp.color_ramp.elements[1].color = (*linear_color, 1.0)
    emission = nodes.new("ShaderNodeEmission")
    emission.inputs["Strength"].default_value = 1.0
    links.new(diffuse.outputs["BSDF"], shader_to_rgb.inputs["Shader"])
    links.new(shader_to_rgb.outputs["Color"], ramp.inputs["Fac"])
    links.new(ramp.outputs["Color"], emission.inputs["Color"])
    links.new(emission.outputs["Emission"], output.inputs["Surface"])
    return mat


def add_uv(name, location, scale, mat, segments=32, rings=16):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments, ring_count=rings, location=location)
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    obj.data.materials.append(mat)
    bpy.ops.object.shade_smooth()
    return obj


def add_tapered_head(name, location, scale, mat):
    obj = add_uv(name, location, scale, mat, segments=40, rings=24)
    half_height = scale[2]
    for vertex in obj.data.vertices:
        normalized_z = max(-1.0, min(1.0, vertex.co.z / half_height))
        if normalized_z < 0.0:
            taper = 1.0 + (0.18 * normalized_z)
            vertex.co.x *= taper
            vertex.co.y *= 0.96 + (0.04 * taper)
    return obj


def add_cube(name, location, scale, mat, bevel=0.08, rotation=(0.0, 0.0, 0.0)):
    bpy.ops.mesh.primitive_cube_add(location=location, rotation=rotation)
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if bevel > 0:
        modifier = obj.modifiers.new("SoftEdges", "BEVEL")
        modifier.width = bevel
        modifier.segments = 2
    obj.data.materials.append(mat)
    return obj


def add_tapered_box(name, location, bottom_half, top_half, height, mat, bevel=0.06):
    bx, by = bottom_half
    tx, ty = top_half
    half_height = height * 0.5
    vertices = [
        (-bx, -by, -half_height), (bx, -by, -half_height),
        (bx, by, -half_height), (-bx, by, -half_height),
        (-tx, -ty, half_height), (tx, -ty, half_height),
        (tx, ty, half_height), (-tx, ty, half_height),
    ]
    faces = [
        (0, 1, 2, 3), (4, 7, 6, 5),
        (0, 4, 5, 1), (1, 5, 6, 2),
        (2, 6, 7, 3), (4, 0, 3, 7),
    ]
    mesh = bpy.data.meshes.new(name + "Mesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    obj.location = location
    obj.data.materials.append(mat)
    if bevel > 0:
        modifier = obj.modifiers.new("BodySoftEdges", "BEVEL")
        modifier.width = bevel
        modifier.segments = 3
    return obj


def add_cape_tail(name, side, mat):
    rows = [
        ((0.24, 0.34, 3.16), (0.56, 0.38, 3.08)),
        ((0.42, 0.38, 2.82), (0.92, 0.46, 2.88)),
        ((0.58, 0.44, 2.42), (1.28, 0.56, 2.58)),
        ((0.72, 0.49, 2.00), (1.52, 0.64, 2.18)),
        ((0.86, 0.51, 1.55), (1.62, 0.68, 1.72)),
        ((1.00, 0.49, 1.12), (1.49, 0.64, 1.18)),
        ((1.12, 0.44, 0.76), (1.31, 0.55, 0.70)),
    ]
    vertices = []
    for inner, outer in rows:
        vertices.append((inner[0] * side, inner[1], inner[2]))
        vertices.append((outer[0] * side, outer[1], outer[2]))
    faces = []
    for row_index in range(len(rows) - 1):
        start = row_index * 2
        faces.append((start, start + 1, start + 3, start + 2))
    mesh = bpy.data.meshes.new(name + "Mesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    obj.data.materials.append(mat)
    solidify = obj.modifiers.new("CapeThickness", "SOLIDIFY")
    solidify.thickness = 0.09
    bevel = obj.modifiers.new("CapeSoftEdges", "BEVEL")
    bevel.width = 0.035
    bevel.segments = 2
    return obj


def add_cylinder_between(name, start, end, radius, mat):
    start_v = Vector(start)
    end_v = Vector(end)
    direction = end_v - start_v
    midpoint = (start_v + end_v) * 0.5
    bpy.ops.mesh.primitive_cylinder_add(vertices=20, radius=radius, depth=direction.length, location=midpoint)
    obj = bpy.context.object
    obj.name = name
    obj.rotation_mode = "QUATERNION"
    obj.rotation_quaternion = direction.to_track_quat("Z", "Y")
    obj.data.materials.append(mat)
    bpy.ops.object.shade_smooth()
    return obj


def add_open_hand(name, side, location, skin_mat):
    root = bpy.data.objects.new(name, None)
    bpy.context.collection.objects.link(root)
    palm = add_uv(name + "Palm", location, (0.16, 0.11, 0.18), skin_mat, segments=20, rings=12)
    hand_parts = []
    finger_specs = [
        ((0.07, -0.02, 0.05), (0.22, -0.035, -0.01), 0.026),
        ((0.08, -0.02, 0.00), (0.25, -0.040, -0.09), 0.027),
        ((0.08, -0.02, -0.05), (0.24, -0.040, -0.17), 0.026),
        ((0.06, -0.02, -0.09), (0.20, -0.035, -0.23), 0.024),
    ]
    for index, (start_offset, end_offset, radius) in enumerate(finger_specs):
        start = (location[0] + start_offset[0] * side, location[1] + start_offset[1], location[2] + start_offset[2])
        end = (location[0] + end_offset[0] * side, location[1] + end_offset[1], location[2] + end_offset[2])
        finger = add_cylinder_between(f"{name}Finger{index + 1}", start, end, radius, skin_mat)
        hand_parts.append(finger)
    thumb_start = (location[0] - 0.08 * side, location[1] - 0.02, location[2] - 0.01)
    thumb_end = (location[0] - 0.20 * side, location[1] - 0.04, location[2] - 0.11)
    thumb = add_cylinder_between(name + "Thumb", thumb_start, thumb_end, 0.035, skin_mat)
    hand_parts.append(thumb)
    for index, part in enumerate(hand_parts):
        bpy.ops.object.select_all(action="DESELECT")
        palm.select_set(True)
        bpy.context.view_layer.objects.active = palm
        modifier = palm.modifiers.new(f"HandUnion{index}", "BOOLEAN")
        modifier.operation = "UNION"
        modifier.solver = "EXACT"
        modifier.object = part
        bpy.ops.object.modifier_apply(modifier=modifier.name)
        bpy.data.objects.remove(part, do_unlink=True)
    palm.name = name + "Mesh"
    palm.parent = root
    return root


def add_cone(name, location, radius1, radius2, depth, mat, rotation=(0.0, 0.0, 0.0), vertices=20):
    bpy.ops.mesh.primitive_cone_add(vertices=vertices, radius1=radius1, radius2=radius2, depth=depth, location=location, rotation=rotation)
    obj = bpy.context.object
    obj.name = name
    obj.data.materials.append(mat)
    return obj


def add_lightning(name, location, scale, mat, mirror=False, thickness=0.14):
    direction = -1.0 if mirror else 1.0
    vertices = [
        (-0.12 * direction, 0.0, 0.58),
        (0.16 * direction, 0.0, 0.20),
        (0.02 * direction, 0.0, 0.20),
        (0.20 * direction, 0.0, -0.58),
        (-0.18 * direction, 0.0, -0.08),
        (-0.02 * direction, 0.0, -0.08),
    ]
    mesh = bpy.data.meshes.new(name + "Mesh")
    mesh.from_pydata(vertices, [], [[0, 1, 2, 3, 4, 5]])
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    obj.location = location
    obj.scale = scale
    obj.data.materials.append(mat)
    solidify = obj.modifiers.new("Thickness", "SOLIDIFY")
    solidify.thickness = thickness
    bevel = obj.modifiers.new("EdgeSoftness", "BEVEL")
    bevel.width = 0.025
    bevel.segments = 2
    return obj


def add_lightning_prism(name, location, scale, depth, mat, mirror=False):
    direction = -1.0 if mirror else 1.0
    outline = [
        (-0.12 * direction, 0.58), (0.16 * direction, 0.20),
        (0.02 * direction, 0.20), (0.20 * direction, -0.58),
        (-0.18 * direction, -0.08), (-0.02 * direction, -0.08),
    ]
    half_depth = depth * 0.5
    vertices = []
    for y in (-half_depth, half_depth):
        vertices.extend([(x * scale, y, z * scale) for x, z in outline])
    front_faces = [(0, 1, 2, 3, 4, 5)]
    back_faces = [(11, 10, 9, 8, 7, 6)]
    side_faces = [(index, (index + 1) % 6, ((index + 1) % 6) + 6, index + 6) for index in range(6)]
    mesh = bpy.data.meshes.new(name + "Mesh")
    mesh.from_pydata(vertices, [], front_faces + back_faces + side_faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    obj.location = location
    obj.data.materials.append(mat)
    bevel = obj.modifiers.new("LightningEdges", "BEVEL")
    bevel.width = 0.025
    bevel.segments = 2
    return obj


def add_panel(name, vertices, mat, thickness=0.06, bevel_width=0.025):
    mesh = bpy.data.meshes.new(name + "Mesh")
    mesh.from_pydata(vertices, [], [list(range(len(vertices)))])
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    obj.data.materials.append(mat)
    solidify = obj.modifiers.new("Thickness", "SOLIDIFY")
    solidify.thickness = thickness
    bevel = obj.modifiers.new("EdgeSoftness", "BEVEL")
    bevel.width = bevel_width
    bevel.segments = 2
    return obj


def add_beveled_polyline(name, points, radius, mat, resolution=2):
    root = bpy.data.objects.new(name, None)
    bpy.context.collection.objects.link(root)
    for index in range(len(points) - 1):
        start_v = Vector(points[index])
        end_v = Vector(points[index + 1])
        direction = end_v - start_v
        midpoint = (start_v + end_v) * 0.5
        bpy.ops.mesh.primitive_cube_add(location=midpoint)
        segment = bpy.context.object
        segment.name = f"{name}_Segment{index}"
        segment.scale = (radius, radius, direction.length * 0.5)
        bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
        segment.rotation_mode = "QUATERNION"
        segment.rotation_quaternion = direction.to_track_quat("Z", "Y")
        segment.data.materials.append(mat)
        bevel = segment.modifiers.new("AngularEdge", "BEVEL")
        bevel.width = radius * 0.18
        bevel.segments = 1
        segment.parent = root
    return root


def look_at(obj, point):
    obj.rotation_euler = (Vector(point) - obj.location).to_track_quat("-Z", "Y").to_euler()


def build_character():
    skin = material("Skin", (0.96, 0.55, 0.43))
    skin_light = material("SkinLight", (1.0, 0.78, 0.66))
    hair = material("HairPurple", (0.455, 0.267, 0.690), roughness=0.55)
    hair_dark = material("HairDark", (0.231, 0.125, 0.400), roughness=0.58)
    hair_light = material("HairHighlight", (0.608, 0.357, 0.859), roughness=0.5)
    costume = material("CostumeBlack", (0.090, 0.075, 0.122), roughness=0.6)
    purple = material("CostumePurple", (0.329, 0.188, 0.561), roughness=0.55)
    bright_purple = material("GemPurple", (0.753, 0.502, 1.0), metallic=0.15, roughness=0.28)
    gold = material("GoldTrim", (0.843, 0.604, 0.090), metallic=0.55, roughness=0.32)
    eye_white = material("EyeWhite", (0.95, 0.95, 1.0), roughness=0.4)
    eye_purple = material("EyePurple", (0.48, 0.12, 0.78), roughness=0.3)
    eye_dark = material("EyeDark", (0.045, 0.015, 0.075), roughness=0.35)
    eye_glint = material("EyeGlint", (1.0, 0.92, 1.0), roughness=0.2)

    root = bpy.data.objects.new("Arca_ModelRoot", None)
    bpy.context.collection.objects.link(root)

    # SD body proportions: large head, compact torso, short limbs.
    head = add_tapered_head("Head", (0.0, -0.18, 4.15), (0.94, 0.70, 0.88), skin_light)
    torso = add_tapered_box("CroppedTop", (0.0, 0.02, 2.88), (0.43, 0.27), (0.57, 0.32), 0.76, costume, bevel=0.075)
    add_tapered_box("Midriff", (0.0, 0.01, 2.43), (0.43, 0.27), (0.38, 0.25), 0.34, skin_light, bevel=0.09)
    pelvis = add_tapered_box("Pelvis", (0.0, 0.03, 2.12), (0.55, 0.34), (0.43, 0.28), 0.30, costume, bevel=0.07)

    # Hair cap and readable side locks.
    add_uv("HairBack", (0.0, 0.28, 4.35), (1.02, 0.62, 0.98), hair)
    add_uv("HairBackShadow", (0.0, 0.48, 4.12), (0.94, 0.44, 0.79), hair_dark)
    add_uv("HairTop", (0.0, -0.10, 4.73), (0.96, 0.61, 0.50), hair)
    add_uv("HairSideL", (-0.73, -0.06, 4.20), (0.36, 0.62, 0.72), hair)
    add_uv("HairSideR", (0.73, -0.06, 4.20), (0.36, 0.62, 0.72), hair)
    # Layered pointed bangs frame the eyes instead of covering them.
    add_panel("BangCenter", [(-0.14, -0.88, 4.91), (0.18, -0.88, 4.91), (0.06, -0.96, 4.28), (-0.07, -0.96, 4.39)], hair_light, thickness=0.18, bevel_width=0.035)
    add_panel("BangL", [(-0.69, -0.87, 4.79), (-0.17, -0.91, 4.92), (-0.28, -0.97, 4.39), (-0.59, -0.96, 4.22), (-0.47, -0.94, 4.50)], hair, thickness=0.18, bevel_width=0.035)
    add_panel("BangR", [(0.16, -0.91, 4.92), (0.69, -0.87, 4.77), (0.57, -0.95, 4.24), (0.29, -0.97, 4.40)], hair, thickness=0.18, bevel_width=0.035)
    add_panel("BangOuterL", [(-0.88, -0.82, 4.68), (-0.62, -0.88, 4.80), (-0.68, -0.94, 4.17), (-0.84, -0.91, 4.02)], hair_dark, thickness=0.16, bevel_width=0.03)
    add_panel("BangOuterR", [(0.62, -0.88, 4.78), (0.88, -0.82, 4.66), (0.83, -0.91, 4.04), (0.68, -0.94, 4.20)], hair_dark, thickness=0.16, bevel_width=0.03)
    add_panel("BangInnerL", [(-0.48, -0.99, 4.78), (-0.12, -1.00, 4.88), (-0.22, -1.02, 4.30), (-0.40, -1.02, 4.48)], hair_light, thickness=0.10, bevel_width=0.025)
    add_panel("BangInnerR", [(0.16, -1.00, 4.86), (0.52, -0.99, 4.72), (0.38, -1.02, 4.39), (0.23, -1.02, 4.48)], hair_dark, thickness=0.10, bevel_width=0.025)
    add_panel("HairHighlightCenter", [(-0.10, -1.035, 4.84), (0.01, -1.035, 4.86), (-0.03, -1.045, 4.52), (-0.11, -1.045, 4.47)], hair_light, thickness=0.018, bevel_width=0.008)
    add_panel("HairHighlightL", [(-0.55, -1.025, 4.70), (-0.44, -1.025, 4.77), (-0.49, -1.038, 4.48), (-0.57, -1.038, 4.41)], hair_light, thickness=0.018, bevel_width=0.008)
    add_panel("HairHighlightR", [(0.34, -1.025, 4.76), (0.45, -1.025, 4.70), (0.42, -1.038, 4.47), (0.34, -1.038, 4.51)], hair_light, thickness=0.018, bevel_width=0.008)
    add_uv("BangRootL", (-0.43, -0.69, 4.67), (0.43, 0.25, 0.28), hair, segments=20, rings=12)
    add_uv("BangRootR", (0.43, -0.69, 4.65), (0.43, 0.25, 0.28), hair, segments=20, rings=12)
    add_cone("HairLockL", (-0.83, -0.10, 3.86), 0.27, 0.07, 1.05, hair, rotation=(0.08, 0.0, -0.20))
    add_cone("HairLockR", (0.83, -0.10, 3.86), 0.27, 0.07, 1.05, hair, rotation=(0.08, 0.0, 0.20))
    add_cone("HairTipL1", (-0.92, 0.10, 4.02), 0.18, 0.035, 0.72, hair_dark, rotation=(0.05, 0.18, -0.42))
    add_cone("HairTipL2", (-0.68, 0.30, 3.70), 0.16, 0.03, 0.62, hair, rotation=(0.10, 0.12, -0.18))
    add_cone("HairTipR1", (0.92, 0.10, 4.02), 0.18, 0.035, 0.72, hair_dark, rotation=(0.05, -0.18, 0.42))
    add_cone("HairTipR2", (0.68, 0.30, 3.70), 0.16, 0.03, 0.62, hair, rotation=(0.10, -0.12, 0.18))
    add_cone("HairTipL3", (-1.00, 0.16, 4.36), 0.16, 0.025, 0.58, hair_light, rotation=(0.03, 0.20, -0.62))
    add_cone("HairTipR3", (1.00, 0.16, 4.36), 0.16, 0.025, 0.58, hair_light, rotation=(0.03, -0.20, 0.62))
    add_cone("HairNapeL", (-0.43, 0.46, 3.75), 0.20, 0.035, 0.62, hair_dark, rotation=(0.12, 0.0, -0.12))
    add_cone("HairNapeR", (0.43, 0.46, 3.75), 0.20, 0.035, 0.62, hair_dark, rotation=(0.12, 0.0, 0.12))
    add_lightning("LightningForelock", (0.0, -0.06, 5.42), (0.72, 0.72, 0.72), hair_light, thickness=0.18)
    forelock_side = add_lightning("LightningForelockSide", (0.0, -0.06, 5.42), (0.54, 0.72, 0.72), hair_dark, thickness=0.10)
    forelock_side.rotation_euler.z = math.radians(90.0)
    ornament_path = [
        (0.68, -0.86, 4.88), (0.90, -1.02, 4.65),
        (0.73, -0.90, 4.61), (0.87, -1.02, 4.47),
        (0.70, -0.92, 4.43), (0.93, -1.06, 4.20),
    ]
    add_beveled_polyline("HairOrnamentGold3D", ornament_path, 0.10, gold)
    add_beveled_polyline("HairOrnamentPurpleCore", ornament_path, 0.042, purple)
    add_uv("HairOrnamentGem", (0.78, -1.12, 4.55), (0.075, 0.020, 0.10), bright_purple, segments=12, rings=8)

    # Face planes are slightly in front of the head for a clean orthographic read.
    add_uv("EyeLWhite", (-0.36, -0.935, 4.12), (0.20, 0.010, 0.17), eye_white, segments=20, rings=12)
    add_uv("EyeRWhite", (0.36, -0.935, 4.12), (0.20, 0.010, 0.17), eye_white, segments=20, rings=12)
    add_uv("EyeLIris", (-0.34, -0.951, 4.08), (0.115, 0.008, 0.145), eye_purple, segments=16, rings=10)
    add_uv("EyeRIris", (0.34, -0.951, 4.08), (0.115, 0.008, 0.145), eye_purple, segments=16, rings=10)
    add_uv("EyeLPupil", (-0.34, -0.962, 4.08), (0.045, 0.005, 0.082), eye_dark, segments=12, rings=8)
    add_uv("EyeRPupil", (0.34, -0.962, 4.08), (0.045, 0.005, 0.082), eye_dark, segments=12, rings=8)
    add_uv("EyeLGlint", (-0.38, -0.969, 4.16), (0.024, 0.004, 0.032), eye_glint, segments=10, rings=6)
    add_uv("EyeRGlint", (0.30, -0.969, 4.16), (0.024, 0.004, 0.032), eye_glint, segments=10, rings=6)
    add_cube("EyeLashL", (-0.36, -0.965, 4.29), (0.22, 0.008, 0.016), eye_dark, bevel=0.006, rotation=(0.0, 0.0, -0.08))
    add_cube("EyeLashR", (0.36, -0.965, 4.29), (0.22, 0.008, 0.016), eye_dark, bevel=0.006, rotation=(0.0, 0.0, 0.08))
    add_uv("CheekL", (-0.52, -0.955, 3.88), (0.13, 0.018, 0.055), skin, segments=12, rings=8)
    add_uv("CheekR", (0.52, -0.955, 3.88), (0.13, 0.018, 0.055), skin, segments=12, rings=8)
    add_cube("Mouth", (0.0, -0.98, 3.78), (0.08, 0.018, 0.014), skin, bevel=0.012)

    # Shoulder armor, arms, guards, and hands.
    add_cube("ShoulderArmorL", (-0.70, -0.05, 2.96), (0.32, 0.27, 0.15), costume, bevel=0.06, rotation=(0.0, 0.12, -0.18))
    add_cube("ShoulderArmorR", (0.70, -0.05, 2.96), (0.32, 0.27, 0.15), costume, bevel=0.06, rotation=(0.0, -0.12, 0.18))
    add_cube("ShoulderTrimL", (-0.70, -0.31, 2.98), (0.34, 0.035, 0.045), gold, bevel=0.02, rotation=(0.0, 0.0, -0.18))
    add_cube("ShoulderTrimR", (0.70, -0.31, 2.98), (0.34, 0.035, 0.045), gold, bevel=0.02, rotation=(0.0, 0.0, 0.18))
    add_uv("ShoulderGemL", (-0.70, -0.35, 2.99), (0.10, 0.035, 0.08), bright_purple, segments=12, rings=8)
    add_uv("ShoulderGemR", (0.70, -0.35, 2.99), (0.10, 0.035, 0.08), bright_purple, segments=12, rings=8)
    add_panel("ShoulderPointL", [(-0.46, -0.34, 3.10), (-1.03, -0.32, 3.01), (-0.73, -0.34, 2.80)], costume, thickness=0.05, bevel_width=0.015)
    add_panel("ShoulderPointR", [(0.46, -0.34, 3.10), (1.03, -0.32, 3.01), (0.73, -0.34, 2.80)], costume, thickness=0.05, bevel_width=0.015)
    add_panel("CollarL", [(-0.05, -0.43, 3.25), (-0.60, -0.41, 3.15), (-0.34, -0.43, 2.88)], costume, thickness=0.045, bevel_width=0.015)
    add_panel("CollarR", [(0.05, -0.43, 3.25), (0.60, -0.41, 3.15), (0.34, -0.43, 2.88)], costume, thickness=0.045, bevel_width=0.015)
    add_panel("CollarGoldL", [(-0.05, -0.48, 3.24), (-0.57, -0.47, 3.14), (-0.52, -0.47, 3.08), (-0.08, -0.48, 3.16)], gold, thickness=0.018, bevel_width=0.006)
    add_panel("CollarGoldR", [(0.05, -0.48, 3.24), (0.57, -0.47, 3.14), (0.52, -0.47, 3.08), (0.08, -0.48, 3.16)], gold, thickness=0.018, bevel_width=0.006)
    add_cylinder_between("UpperArmL", (-0.66, 0.0, 2.78), (-0.92, -0.03, 2.26), 0.14, skin)
    add_cylinder_between("UpperArmR", (0.66, 0.0, 2.78), (0.92, -0.03, 2.26), 0.14, skin)
    add_cylinder_between("ForearmGuardL", (-0.92, -0.03, 2.26), (-1.02, -0.08, 1.84), 0.18, costume)
    add_cylinder_between("ForearmGuardR", (0.92, -0.03, 2.26), (1.02, -0.08, 1.84), 0.18, costume)
    add_open_hand("HandL", -1.0, (-1.04, -0.10, 1.67), skin_light)
    add_open_hand("HandR", 1.0, (1.04, -0.10, 1.67), skin_light)
    add_uv("WristGemL", (-0.95, -0.30, 2.08), (0.12, 0.06, 0.11), bright_purple)
    add_uv("WristGemR", (0.95, -0.30, 2.08), (0.12, 0.06, 0.11), bright_purple)
    add_cube("GuardTrimL", (-0.98, -0.22, 2.02), (0.20, 0.035, 0.025), gold, bevel=0.01, rotation=(0.0, 0.0, -0.10))
    add_cube("GuardTrimR", (0.98, -0.22, 2.02), (0.20, 0.035, 0.025), gold, bevel=0.01, rotation=(0.0, 0.0, 0.10))
    add_panel("GuardPlateL", [(-1.16, -0.25, 2.28), (-0.92, -0.28, 2.43), (-0.81, -0.27, 2.00), (-1.03, -0.28, 1.82)], costume, thickness=0.035, bevel_width=0.012)
    add_panel("GuardPlateR", [(1.16, -0.25, 2.28), (0.92, -0.28, 2.43), (0.81, -0.27, 2.00), (1.03, -0.28, 1.82)], costume, thickness=0.035, bevel_width=0.012)

    # Chest gem, belt, layered skirt.
    add_panel("ChestGem", [(0.0, -0.43, 3.17), (0.19, -0.43, 2.95), (0.0, -0.45, 2.73), (-0.19, -0.43, 2.95)], bright_purple, thickness=0.07, bevel_width=0.025)
    add_cube("ChestGoldL", (-0.20, -0.38, 2.93), (0.20, 0.025, 0.035), gold, bevel=0.015, rotation=(0.0, 0.0, -0.42))
    add_cube("ChestGoldR", (0.20, -0.38, 2.93), (0.20, 0.025, 0.035), gold, bevel=0.015, rotation=(0.0, 0.0, 0.42))
    add_cube("TopHem", (0.0, -0.34, 2.55), (0.48, 0.035, 0.035), gold, bevel=0.015)
    add_cube("NeckGuard", (0.0, -0.18, 3.23), (0.24, 0.18, 0.08), costume, bevel=0.035)
    add_panel("TopPurpleInset", [(-0.38, -0.405, 3.10), (0.38, -0.405, 3.10), (0.25, -0.405, 2.62), (-0.25, -0.405, 2.62)], purple, thickness=0.035, bevel_width=0.015)
    add_cube("Belt", (0.0, -0.03, 2.14), (0.66, 0.39, 0.09), gold, bevel=0.04)
    add_cone("SkirtOuter", (0.0, 0.08, 1.82), 0.76, 0.53, 0.66, costume, vertices=10)
    add_cone("SkirtPurple", (0.0, 0.05, 1.66), 0.70, 0.50, 0.42, purple, vertices=10)
    add_cube("SkirtGoldL", (-0.53, -0.26, 1.69), (0.25, 0.025, 0.035), gold, bevel=0.01, rotation=(0.0, 0.0, -0.42))
    add_cube("SkirtGoldR", (0.53, -0.26, 1.69), (0.25, 0.025, 0.035), gold, bevel=0.01, rotation=(0.0, 0.0, 0.42))
    add_panel("SkirtFrontPanel", [(-0.30, -0.43, 2.02), (0.30, -0.43, 2.02), (0.45, -0.43, 1.48), (0.0, -0.45, 1.34), (-0.45, -0.43, 1.48)], costume, thickness=0.05, bevel_width=0.018)
    add_panel("SkirtFrontPurple", [(-0.18, -0.47, 1.94), (0.18, -0.47, 1.94), (0.28, -0.47, 1.52), (0.0, -0.48, 1.43), (-0.28, -0.47, 1.52)], purple, thickness=0.025, bevel_width=0.012)
    add_panel("SkirtHemL", [(-0.72, -0.49, 1.62), (-0.24, -0.49, 1.48), (-0.46, -0.50, 1.34)], bright_purple, thickness=0.025, bevel_width=0.01)
    add_panel("SkirtHemC", [(-0.27, -0.50, 1.49), (0.27, -0.50, 1.49), (0.0, -0.51, 1.29)], bright_purple, thickness=0.025, bevel_width=0.01)
    add_panel("SkirtHemR", [(0.24, -0.49, 1.48), (0.72, -0.49, 1.62), (0.46, -0.50, 1.34)], bright_purple, thickness=0.025, bevel_width=0.01)
    add_panel("SkirtSidePlateL", [(-0.48, -0.45, 2.02), (-0.92, -0.30, 1.88), (-0.72, -0.34, 1.28), (-0.46, -0.46, 1.48)], costume, thickness=0.045, bevel_width=0.015)
    add_panel("SkirtSidePlateR", [(0.48, -0.45, 2.02), (0.92, -0.30, 1.88), (0.72, -0.34, 1.28), (0.46, -0.46, 1.48)], costume, thickness=0.045, bevel_width=0.015)
    add_panel("SkirtSideGemL", [(-0.72, -0.38, 1.75), (-0.61, -0.40, 1.58), (-0.72, -0.40, 1.42), (-0.82, -0.38, 1.59)], bright_purple, thickness=0.025, bevel_width=0.008)
    add_panel("SkirtSideGemR", [(0.72, -0.38, 1.75), (0.61, -0.40, 1.58), (0.72, -0.40, 1.42), (0.82, -0.38, 1.59)], bright_purple, thickness=0.025, bevel_width=0.008)
    add_uv("BeltGem", (0.36, -0.43, 2.12), (0.11, 0.045, 0.11), bright_purple, segments=12, rings=8)
    add_cube("DiagonalBelt", (0.0, -0.44, 2.14), (0.69, 0.035, 0.055), costume, bevel=0.018, rotation=(0.0, 0.0, -0.17))
    add_cube("DiagonalBeltTrim", (0.0, -0.485, 2.14), (0.70, 0.018, 0.018), gold, bevel=0.008, rotation=(0.0, 0.0, -0.17))
    add_panel("BeltBuckle", [(0.26, -0.51, 2.27), (0.42, -0.51, 2.20), (0.39, -0.51, 2.03), (0.23, -0.51, 2.10)], gold, thickness=0.035, bevel_width=0.01)

    # Legs and armored boots.
    add_cylinder_between("ThighL", (-0.28, 0.02, 1.63), (-0.31, 0.0, 1.02), 0.17, skin_light)
    add_cylinder_between("ThighR", (0.28, 0.02, 1.63), (0.31, 0.0, 1.02), 0.17, skin_light)
    add_cylinder_between("BootL", (-0.31, 0.0, 1.02), (-0.34, -0.05, 0.36), 0.22, costume)
    add_cylinder_between("BootR", (0.31, 0.0, 1.02), (0.34, -0.05, 0.36), 0.22, costume)
    add_uv("BootFootL", (-0.34, -0.18, 0.22), (0.26, 0.36, 0.18), costume)
    add_uv("BootFootR", (0.34, -0.18, 0.22), (0.26, 0.36, 0.18), costume)
    add_cube("BootTrimL", (-0.34, -0.30, 0.92), (0.27, 0.035, 0.045), gold, bevel=0.02)
    add_cube("BootTrimR", (0.34, -0.30, 0.92), (0.27, 0.035, 0.045), gold, bevel=0.02)
    add_panel("BootPlateL", [(-0.55, -0.31, 0.95), (-0.34, -0.36, 1.18), (-0.13, -0.31, 0.95), (-0.34, -0.34, 0.62)], costume, thickness=0.05, bevel_width=0.018)
    add_panel("BootPlateR", [(0.13, -0.31, 0.95), (0.34, -0.36, 1.18), (0.55, -0.31, 0.95), (0.34, -0.34, 0.62)], costume, thickness=0.05, bevel_width=0.018)
    add_uv("BootGemL", (-0.34, -0.32, 0.78), (0.15, 0.07, 0.18), bright_purple)
    add_uv("BootGemR", (0.34, -0.32, 0.78), (0.15, 0.07, 0.18), bright_purple)
    add_panel("BootGoldL", [(-0.53, -0.37, 1.00), (-0.34, -0.39, 1.22), (-0.15, -0.37, 1.00), (-0.34, -0.39, 0.57)], gold, thickness=0.022, bevel_width=0.008)
    add_panel("BootGoldR", [(0.15, -0.37, 1.00), (0.34, -0.39, 1.22), (0.53, -0.37, 1.00), (0.34, -0.39, 0.57)], gold, thickness=0.022, bevel_width=0.008)
    add_uv("BootGemFrontL", (-0.34, -0.43, 0.83), (0.105, 0.035, 0.15), bright_purple, segments=12, rings=8)
    add_uv("BootGemFrontR", (0.34, -0.43, 0.83), (0.105, 0.035, 0.15), bright_purple, segments=12, rings=8)
    add_cube("BootToeTrimL", (-0.34, -0.48, 0.20), (0.24, 0.025, 0.025), gold, bevel=0.008)
    add_cube("BootToeTrimR", (0.34, -0.48, 0.20), (0.24, 0.025, 0.025), gold, bevel=0.008)

    # Split cape panels follow the wide, wing-like silhouette of the Design Master.
    cape_left = [
        (-0.30, 0.34, 3.16), (-0.74, 0.38, 3.02), (-1.16, 0.48, 2.72),
        (-1.48, 0.58, 2.26), (-1.66, 0.64, 1.70), (-1.58, 0.62, 1.18),
        (-1.35, 0.54, 0.74), (-1.18, 0.48, 1.28), (-0.86, 0.40, 1.72),
        (-0.56, 0.35, 2.18),
    ]
    cape_right = [(-x, y, z) for x, y, z in cape_left]
    add_cape_tail("CapeTailL", -1.0, purple)
    add_cape_tail("CapeTailR", 1.0, purple)
    cape_trim_left = [
        (-1.65, 0.43, 1.72), (-1.36, 0.43, 0.76), (-1.18, 0.43, 1.28), (-1.53, 0.43, 1.78),
    ]
    cape_trim_right = [(-x, y, z) for x, y, z in cape_trim_left]
    add_panel("CapeGlowL", cape_trim_left, bright_purple, thickness=0.04, bevel_width=0.015)
    add_panel("CapeGlowR", cape_trim_right, bright_purple, thickness=0.04, bevel_width=0.015)
    cape_inner_left = [(-0.48, 0.39, 2.91), (-0.93, 0.39, 2.56), (-1.37, 0.39, 1.75), (-1.30, 0.39, 1.22), (-1.07, 0.39, 1.63), (-0.71, 0.39, 2.18)]
    cape_inner_right = [(-x, y, z) for x, y, z in cape_inner_left]
    add_panel("CapeInnerL", cape_inner_left, hair_dark, thickness=0.045, bevel_width=0.018)
    add_panel("CapeInnerR", cape_inner_right, hair_dark, thickness=0.045, bevel_width=0.018)
    cape_gold_left = [(-0.38, 0.42, 3.08), (-0.80, 0.42, 2.90), (-0.77, 0.42, 2.78), (-0.38, 0.42, 2.94)]
    cape_gold_right = [(-x, y, z) for x, y, z in cape_gold_left]
    add_panel("CapeGoldL", cape_gold_left, gold, thickness=0.035, bevel_width=0.012)
    add_panel("CapeGoldR", cape_gold_right, gold, thickness=0.035, bevel_width=0.012)
    add_panel("BackEmblem", [(0.0, 0.62, 3.04), (0.20, 0.62, 2.84), (0.0, 0.63, 2.62), (-0.20, 0.62, 2.84)], gold, thickness=0.04, bevel_width=0.012)
    add_panel("BackEmblemGem", [(0.0, 0.68, 2.98), (0.11, 0.68, 2.84), (0.0, 0.69, 2.69), (-0.11, 0.68, 2.84)], bright_purple, thickness=0.025, bevel_width=0.008)

    for obj in bpy.context.scene.objects:
        if obj != root and obj.type in {"MESH", "EMPTY"} and obj.parent is None:
            obj.parent = root

    return root


def configure_scene():
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x = 512
    scene.render.resolution_y = 512
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.image_settings.color_mode = "RGBA"
    scene.render.film_transparent = True
    scene.render.filepath = str(PREVIEW_PATH)
    scene.view_settings.view_transform = "Standard"
    scene.view_settings.look = "None"
    scene.view_settings.exposure = 0.0
    scene.view_settings.gamma = 1.0
    scene.render.use_freestyle = True
    scene.render.line_thickness = 0.8

    world = bpy.data.worlds.new("ArcaWorld") if bpy.data.worlds.get("ArcaWorld") is None else bpy.data.worlds["ArcaWorld"]
    world.color = (0.02, 0.015, 0.04)
    scene.world = world

    bpy.ops.object.light_add(type="AREA", location=(-3.5, -4.0, 7.0))
    key = bpy.context.object
    key.name = "KeyLight"
    key.data.energy = 320
    key.data.shape = "DISK"
    key.data.size = 5.0
    look_at(key, (0.0, 0.0, 2.8))

    bpy.ops.object.light_add(type="AREA", location=(3.0, -1.5, 4.5))
    fill = bpy.context.object
    fill.name = "PurpleFill"
    fill.data.energy = 140
    fill.data.color = (0.42, 0.16, 0.85)
    fill.data.size = 4.0
    look_at(fill, (0.0, 0.0, 2.8))

    bpy.ops.object.light_add(type="AREA", location=(0.0, 4.5, 5.5))
    rim = bpy.context.object
    rim.name = "BackValidationLight"
    rim.data.energy = 260
    rim.data.color = (0.55, 0.35, 1.0)
    rim.data.size = 5.0
    look_at(rim, (0.0, 0.3, 2.8))

    bpy.ops.object.camera_add(location=(0.0, -12.0, 3.0))
    camera = bpy.context.object
    camera.name = "ArcaRenderCamera"
    camera.data.type = "ORTHO"
    camera.data.ortho_scale = 6.5
    look_at(camera, (0.0, 0.0, 2.7))
    scene.camera = camera


def render_turnaround():
    scene = bpy.context.scene
    camera = bpy.data.objects["ArcaRenderCamera"]
    views = {
        "Front": (0.0, -12.0, 3.0),
        "ThreeQuarter": (7.2, -9.6, 3.0),
        "Side": (12.0, 0.0, 3.0),
        "Back": (0.0, 12.0, 3.0),
    }
    for view_name, location in views.items():
        camera.location = location
        look_at(camera, (0.0, 0.0, 2.7))
        output_path = PREVIEW_ROOT / f"Arca_Master_DetailV14_{view_name}.png"
        scene.render.filepath = str(output_path)
        bpy.ops.render.render(write_still=True)


def main():
    BLENDER_ROOT.mkdir(parents=True, exist_ok=True)
    PREVIEW_ROOT.mkdir(parents=True, exist_ok=True)
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for datablocks in (bpy.data.meshes, bpy.data.curves, bpy.data.materials, bpy.data.cameras, bpy.data.lights):
        for block in list(datablocks):
            if block.users == 0:
                datablocks.remove(block)
    configure_scene()
    build_character()
    bpy.ops.wm.save_as_mainfile(filepath=str(BLEND_PATH))
    render_turnaround()
    bpy.ops.wm.save_as_mainfile(filepath=str(BLEND_PATH))
    print(f"ARCA_BLEND={BLEND_PATH}")
    print(f"ARCA_PREVIEW={PREVIEW_PATH}")


if __name__ == "__main__":
    main()
