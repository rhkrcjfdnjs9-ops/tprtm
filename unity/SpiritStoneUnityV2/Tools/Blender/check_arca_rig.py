import bpy


armature = bpy.data.objects.get("ArcaArmature")
unbound = []
for obj in bpy.data.objects:
    if obj.type != "MESH":
        continue
    modifiers = [modifier for modifier in obj.modifiers if modifier.type == "ARMATURE" and modifier.object == armature]
    if not modifiers:
        unbound.append(obj.name)

print(f"ARMATURE_PRESENT={armature is not None}")
print(f"BONE_COUNT={len(armature.data.bones) if armature else 0}")
print(f"IK_LEFT={bool(armature and armature.pose.bones['Shin.L'].constraints.get('FootIK.L'))}")
print(f"IK_RIGHT={bool(armature and armature.pose.bones['Shin.R'].constraints.get('FootIK.R'))}")
print(f"UNBOUND_COUNT={len(unbound)}")
for name in unbound:
    print(f"UNBOUND={name}")
