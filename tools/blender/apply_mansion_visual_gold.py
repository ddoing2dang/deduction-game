import bpy, os, math, json
from mathutils import Vector

# Mansion Visual Gold auto-apply script.
# Run inside the existing Pass02 mansion .blend after extracting Mansion_CC0_FullPack beside the project.
# Preserves existing room/door/nav geometry and only adds dressing collections, lighting, and exterior dressing.

PACK_ENV = os.environ.get("MANSION_CC0_PACK")
PROJECT_ROOT = os.environ.get("DEDUCTION_GAME_ROOT")
if PACK_ENV:
    PACK = PACK_ENV
elif PROJECT_ROOT:
    PACK = os.path.join(PROJECT_ROOT, "art", "Mansion", "Mansion_CC0_FullPack")
else:
    PACK = os.path.join(os.path.dirname(bpy.data.filepath), "Mansion_CC0_FullPack")

ROOMS = {
    "CENTRAL_HALL": ((0,0,2), 0),
    "STUDY": ((-12,0,4), 0),
    "DRAWING_ROOM": ((12,0,4), 0),
    "DINING": ((-11,0,-7), 0),
    "KITCHEN": ((2,0,-8), 0),
    "GREENHOUSE": ((0,0,13), 0),
    "BOILER": ((-10,-4,-6), 0),
    "WINE": ((1,-4,-7), 0),
    "SERVANT": ((10,-4,-5), 0),
}

PLACEMENTS = [
    ("Chandelier 01", "CENTRAL_HALL", (0,0,5.8), 1.2, 0),
    ("Chandelier 02", "DINING", (0,0,4.8), 0.95, 0),
    ("Arm Chair 01", "DRAWING_ROOM", (-2.2,0.8,0), 1.0, 0.35),
    ("Arm Chair 01", "DRAWING_ROOM", (2.0,0.8,0), 1.0, -0.35),
    ("Gothic Cabinet 01", "STUDY", (-3.0,2.3,0), 1.0, 0),
    ("Gothic Commode 01", "STUDY", (2.5,2.0,0), 1.0, 0),
    ("Gothic Coffee Table", "DRAWING_ROOM", (0,0.4,0), 1.0, 0),
    ("Dining Table", "DINING", (0,0.1,0), 1.15, 0),
    ("Classic Console 01", "CENTRAL_HALL", (3.3,2.0,0), 1.0, 0),
    ("Vintage Grandfather Clock", "CENTRAL_HALL", (-4.2,2.8,0), 1.0, 0),
    ("Fancy Picture Frame 02", "CENTRAL_HALL", (0,4.4,2.1), 1.3, 0),
    ("Wine Barrel 01", "WINE", (-2.4,1.8,0), 1.0, 0),
    ("Wine Barrel 01", "WINE", (2.2,1.7,0), 1.0, 0),
    ("Marble Bust 01", "CENTRAL_HALL", (3.5,-2.8,0.9), 1.0, 0),
    ("Brass Vase 03", "DINING", (3.2,2.2,0.9), 1.0, 0),
    ("Antique Ceramic Vase 01", "DRAWING_ROOM", (-3.2,2.0,0.8), 1.0, 0),
    ("Tea Set 01", "DRAWING_ROOM", (0,0.3,0.65), 1.0, 0),
    ("Classic Nightstand 01", "SERVANT", (2.3,1.8,0), 1.0, 0),
    ("Vintage Oil Lamp", "SERVANT", (2.3,1.8,0.85), 1.0, 0),
    ("Potted Plant 01", "GREENHOUSE", (-2.8,1.9,0), 1.1, 0),
    ("Potted Plant 02", "GREENHOUSE", (2.7,1.8,0), 1.1, 0),
    ("Potted Plant 04", "GREENHOUSE", (0,-2.1,0), 1.2, 0),
]

def norm(s):
    return ''.join(ch.lower() for ch in s if ch.isalnum())

def locate_asset_blend(label):
    target = norm(label)
    candidates = []
    for root, dirs, files in os.walk(PACK):
        for fn in files:
            if fn.lower().endswith(".blend"):
                p = os.path.join(root, fn)
                score = 0
                n = norm(root + " " + fn)
                if target in n:
                    score += 100
                toks = [t for t in target.split() if t]
                score += sum(1 for t in toks if t in n)
                candidates.append((score,p))
    candidates.sort(reverse=True)
    return candidates[0][1] if candidates and candidates[0][0] > 0 else None

def append_all_objects(blend_path, new_collection_name):
    before = set(bpy.data.objects)
    with bpy.data.libraries.load(blend_path, link=False) as (src, dst):
        dst.objects = list(src.objects)
    objs = [o for o in bpy.data.objects if o not in before]
    coll = bpy.data.collections.new(new_collection_name)
    bpy.context.scene.collection.children.link(coll)
    for o in objs:
        for c in list(o.users_collection):
            c.objects.unlink(o)
        coll.objects.link(o)
    return coll, objs

def collection_bbox(objs):
    pts = []
    for o in objs:
        if o.type == 'MESH':
            for c in o.bound_box:
                pts.append(o.matrix_world @ Vector(c))
    if not pts:
        return Vector((0,0,0)), Vector((1,1,1))
    mn = Vector((min(p.x for p in pts), min(p.y for p in pts), min(p.z for p in pts)))
    mx = Vector((max(p.x for p in pts), max(p.y for p in pts), max(p.z for p in pts)))
    return (mn+mx)*0.5, mx-mn

def place_asset(label, room, offset, scale, rz):
    path = locate_asset_blend(label)
    if not path:
        print("SKIP missing", label)
        return
    room_pos = Vector(ROOMS[room][0])
    base = room_pos + Vector(offset)
    coll, objs = append_all_objects(path, "VG_" + norm(label)[:32])
    center, size = collection_bbox(objs)
    root = bpy.data.objects.new("ROOT_" + norm(label)[:32], None)
    coll.objects.link(root)
    for o in objs:
        if o.parent is None:
            o.parent = root
    root.matrix_world.translation = base - center*scale
    root.scale = (scale,scale,scale)
    root.rotation_euler[2] = rz

def ensure_collection(name):
    c = bpy.data.collections.get(name)
    if not c:
        c = bpy.data.collections.new(name)
        bpy.context.scene.collection.children.link(c)
    return c

def add_exterior():
    c = ensure_collection("VG_EXTERIOR")
    def cube(name, loc, scale):
        bpy.ops.mesh.primitive_cube_add(location=loc)
        o=bpy.context.object; o.name=name; o.scale=scale
        for old in list(o.users_collection): old.objects.unlink(o)
        c.objects.link(o)
        return o
    cube("EstateGround", (0,0,-0.35), (30,28,0.3))
    # perimeter walls / gate zone
    cube("Wall_N", (0,27,0.8), (30,0.35,1.2))
    cube("Wall_S_L", (-17,-27,0.8), (13,0.35,1.2))
    cube("Wall_S_R", (17,-27,0.8), (13,0.35,1.2))
    cube("Wall_E", (30,0,0.8), (0.35,27,1.2))
    cube("Wall_W", (-30,0,0.8), (0.35,27,1.2))
    cube("Driveway", (0,-18,-0.02), (5.5,9,0.05))
    # fountain placeholder, replace with asset later if available
    bpy.ops.mesh.primitive_cylinder_add(vertices=48, radius=3.6, depth=0.55, location=(0,-13,0.15))
    f=bpy.context.object; f.name="EstateFountain_Base"
    for old in list(f.users_collection): old.objects.unlink(f)
    c.objects.link(f)
    # hedge masses
    for x in (-23,-18,18,23):
        cube("Hedge", (x,-17,1.0), (2.1,7.5,1.0))
    for x in (-22,-14,-6,6,14,22):
        cube("Hedge_N", (x,22,1.0), (2.8,1.5,1.0))

def add_lights():
    c = ensure_collection("VG_LIGHTING")
    # warm interior key lights
    for name,(p,_) in ROOMS.items():
        x,y,z=p
        data=bpy.data.lights.new("L_"+name, type='AREA')
        data.energy=450 if z>=0 else 250
        data.shape='DISK'; data.size=4.5
        obj=bpy.data.objects.new("L_"+name,data)
        obj.location=(x,y,z+4.2)
        obj.rotation_euler=(0,0,0)
        c.objects.link(obj)
    # moon
    data=bpy.data.lights.new("Moon_Key", type='AREA'); data.energy=900; data.size=18
    obj=bpy.data.objects.new("Moon_Key",data); obj.location=(18,-18,22); obj.rotation_euler=(math.radians(25),0,math.radians(145)); c.objects.link(obj)

for args in PLACEMENTS:
    try:
        place_asset(*args)
    except Exception as e:
        print("PLACE ERROR", args[0], e)

add_exterior()
add_lights()

# Keep original gameplay collections intact; mark Visual Gold additions clearly.
bpy.context.scene["MANSION_VISUAL_GOLD_APPLIED"] = True
bpy.context.scene["MANSION_VISUAL_GOLD_PACK"] = os.path.abspath(PACK)
print("MANSION_VISUAL_GOLD_APPLY: OK")
