正常版海报 — 3 张

01 RIOT / Paramore
02 Seventeen / February 2010
03 Pretty Little Liars

尺寸统一：宽 0.50 m × 高 0.65 m × 厚 0.0008 m。
原点在纸张中心，Y 向上；模型本地 +Z 为印刷面，背面米白色。
每张 16 个三角面，无画框、无装饰、无扭曲。
三张贴图均为用户提供的原始 JPG 文件，字节未改动；没有重新生成文字或人物。
原始分辨率：RIOT 500×500；Seventeen 573×762；PLL 1200×1600。
RIOT 用上下白边适配竖版海报，另外两张只需很窄的左右白边；原图不裁剪、不拉伸。

Unity 使用（推荐）
1. 解压后，把整个 Normal_Posters 文件夹拖入项目 Assets。
2. 等待导入与脚本编译。
3. 菜单点击 Tools > Eco > Create Normal Poster Prefabs。
4. 打开新生成的 Prefabs 文件夹，将三张海报拖入场景。
5. 使用 Transform 调整位置、旋转和缩放。贴墙时离墙面留约 0.005 m 的间距，避免闪烁。
如果看到纯白色背面，将海报绕 Y 轴旋转 180 度。
脚本直接建立 Unity Mesh、材质和 Prefab，不需要其他模型插件。
支持 URP/Lit 或 Built-in Standard；HDRP 材质需手动适配。
脚本只在编辑器中运行；没有自动切场景行为，也没有添加碰撞体。

其他软件
每张提供 OBJ + MTL 和 GLB。
OBJ 与对应 MTL 放在同一目录，并保留 Textures 文件夹。
GLB 内嵌原图贴图；Unity 默认不直接导入 GLB，请使用上述脚本或 OBJ。
双击 Preview.html 可用浏览器查看三张原图的比例和留白布局。
Source/build_posters.py 提供模型生成源码，依赖 Python + Pillow；如需重新运行，将原始上传图片放在同级 upload 文件夹并保留脚本内源文件名。

后续第二场景
本包只包含正常版；三个模型的尺寸、中心点和方向一致，后续可制作匹配的扭曲版并按原 Transform 替换。

检查情况
已验证原图字节一致、尺寸、UV、三角面绕序/法线、OBJ 索引、GLB 二进制结构和 ZIP 解压完整性。
当前环境没有 Unity 编辑器，辅助脚本未在 Unity 内编译运行；如 Unity 出现错误，请提供 Console 截图。
