import * as OBC from "@thatopen/components";
import * as THREE from "three";

const container = document.getElementById("viewer");
const status = document.getElementById("status");
const statusTitle = document.getElementById("status-title");
const statusDetails = document.getElementById("status-details");
const fitButton = document.getElementById("fit-model");

let world;
let loadedObject;

function updateStatus(title, details = "") {
    statusTitle.textContent = title;
    statusDetails.textContent = details;
}

function showError(error) {
    console.error(error);
    status.classList.add("error");
    updateStatus("Не удалось показать IFC", error instanceof Error ? error.message : String(error));
}

async function fitModel() {
    if (!loadedObject || !world) {
        return;
    }

    const box = new THREE.Box3().setFromObject(loadedObject);
    if (box.isEmpty()) {
        return;
    }

    const sphere = box.getBoundingSphere(new THREE.Sphere());
    await world.camera.controls.fitToSphere(sphere, true);
}

async function initialize() {
    const components = new OBC.Components();
    const worlds = components.get(OBC.Worlds);
    world = worlds.create();
    world.scene = new OBC.SimpleScene(components);
    world.scene.setup();
    world.scene.three.background = new THREE.Color(0x10141a);
    world.renderer = new OBC.SimpleRenderer(components, container);
    world.camera = new OBC.OrthoPerspectiveCamera(components);
    components.init();
    components.get(OBC.Grids).create(world);

    const fragments = components.get(OBC.FragmentsManager);
    fragments.init(new URL("./worker.mjs", import.meta.url).href);
    world.camera.controls.addEventListener("update", () => fragments.core.update());
    fragments.list.onItemSet.add(({ value: model }) => {
        model.useCamera(world.camera.three);
        world.scene.three.add(model.object);
        loadedObject = model.object;
        fragments.core.update(true);
    });

    const loader = components.get(OBC.IfcLoader);
    await loader.setup({
        autoSetWasm: false,
        wasm: {
            path: new URL("./wasm/", import.meta.url).href,
            absolute: true
        }
    });

    const response = await fetch("model.ifc", { cache: "no-store" });
    if (!response.ok) {
        throw new Error(`Сервер вернул ${response.status} при загрузке модели.`);
    }

    const data = new Uint8Array(await response.arrayBuffer());
    updateStatus("Преобразование IFC…", "Первое открытие большой модели может занять некоторое время.");
    await loader.load(data, false, "model", {
        processData: {
            progressCallback: progress => {
                const percent = Math.round(progress * 100);
                updateStatus("Преобразование IFC…", `${percent}%`);
            }
        }
    });

    await fitModel();
    fitButton.disabled = false;
    status.classList.add("ready");

    const resizeObserver = new ResizeObserver(() => {
        world.renderer.resize();
    });
    resizeObserver.observe(container);
}

fitButton.addEventListener("click", fitModel);
initialize().catch(showError);
