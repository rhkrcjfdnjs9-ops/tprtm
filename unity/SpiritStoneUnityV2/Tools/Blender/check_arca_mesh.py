import bpy
import bmesh


for obj in bpy.data.objects:
    if obj.type != "MESH":
        continue
    mesh = bmesh.new()
    mesh.from_mesh(obj.data)
    multi_edges = [edge for edge in mesh.edges if len(edge.link_faces) > 2]
    if multi_edges:
        print(f"NON_MANIFOLD_MULTI={obj.name}:{len(multi_edges)}")
    mesh.free()
