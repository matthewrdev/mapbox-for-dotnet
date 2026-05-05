# Mapbox Harness Test Assets

`monte-sordo-mapbox-smoke.glb` is a reduced version of the Monte Sordo GLB used by the iOS and Android smoke harnesses.

Source asset:

`https://redpointwebhosting.z8.web.core.windows.net/uld/0e94eb0d-2e24-4e6d-a715-db5d5951f62f/monte-sordo_uld.glb`

The source GLB required `KHR_draco_mesh_compression` and exported one primitive with 89,974 `POSITION` entries and 32-bit indices. Mapbox rejects that shape because model geometry must fit 16-bit indices.

The checked-in asset was produced with Blender 4.5.1 using the same import, remove-doubles, delete-loose, collapse-decimate, and GLB export flow as the Redpoint asset-processing remesh scripts. The passing export has:

- `POSITION` accessor count: 65,284
- index accessor component type: `UNSIGNED_SHORT`
- texture: original 1024x1024 JPEG base color texture
- `extensionsRequired`: none
