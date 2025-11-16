const isLocal = window.location.hostname === "localhost";
const apiBase = isLocal ? "http://localhost:7040/api" : "/api";
let currentUser = null;

function showOutput(data) {
    document.getElementById("output").classList.remove("hidden");
    document.getElementById("output-text").textContent = JSON.stringify(data, null, 2);
}

async function renderUser(user) {
    const section = document.getElementById("user-info");

    if (!user) {
        section.classList.add("hidden");
        return;
    }

    document.getElementById("user-name").textContent = user.userDetails;

    let roles = await apiGetRaw("/roles");

    document.getElementById("user-role").textContent = roles?.join(", ") || "no roles";
    section.classList.remove("hidden");

    if (!roles?.includes("manager")) {
        document.getElementById("btn-next").style.display = "none";
        document.getElementById("btn-clear-queue").style.display = "none";
    }
}

function showMessage(title, message) {
    document.getElementById("output").classList.remove("hidden");
    document.getElementById("output-title").textContent = title;
    document.getElementById("output-content").innerHTML = `<p>${message}</p>`;
}

function showQueue(title, queueItems) {
    document.getElementById("output").classList.remove("hidden");
    document.getElementById("output-title").textContent = title;

    if (!queueItems || queueItems.length === 0) {
        document.getElementById("output-content").innerHTML = `<p>0 користувачів у черзі.</p>`;
        return;
    }

    let html = `<ol class="list-decimal ml-6">`;
    queueItems.forEach((item, index) => {
        html += `<li class="mb-1">${item.userEmail}</li>`;
    });
    html += `</ol>`;

    document.getElementById("output-content").innerHTML = html;
}

async function apiGetRaw(path) {
    const res = await fetch(apiBase + path);
    const body = await res.text();

    console.log("Status:", res.status);
    console.log("Response body:", body);
    
    return await res.json().catch(() => null);
}

async function apiPostRaw(path, body = null) {
    const res = await fetch(apiBase + path, {
        method: "POST",
        headers: body ? { "Content-Type": "application/json" } : undefined,
        body: body ? JSON.stringify(body) : null
    });
    return await res.json().catch(() => null);
}

async function apiDeleteRaw(path) {
    const res = await fetch(apiBase + path, { method: "DELETE" });
    return await res.json().catch(() => null);
}

async function createQueueItem() {
    if (!currentUser) return;

    const email = currentUser.userDetails;
    const res = await apiPostRaw("/queues", { userEmail: email });

    showMessage("Чергу оновлено", `Користувач ${email} став у чергу.`);
}

async function getQueues() {
    const data = await apiGetRaw("/queues");
    showQueue("Поточна черга", data);
}

async function popNextUser() {
    const data = await apiPostRaw("/queues/pop");

    if (!data || !data.userEmail) {
        showMessage("Черга пуста", "0 користувачів.");
        return;
    }

    showMessage("Наступний користувач", `${data.userEmail}`);
}

async function getUserStatus() {
    if (!currentUser) return;

    const email = currentUser.userDetails;
    const data = await apiGetRaw(`/queues/status/${email}`);

    if (!data || !data.position && data.position !== 0) {
        showMessage("Статус", "Користувач не в черзі.");
        return;
    }

    showMessage("Статус", `Ваш номер в черзі: ${data.position}`);
}

async function clearQueue() {
    await apiDeleteRaw("/queues");
    showMessage("Черга очищена", "Чергу успішно очищено.");
}

async function init() {
    const auth = await fetch("/.auth/me").then(r => r.json()).catch(() => null);
    currentUser = auth?.clientPrincipal || null;

    await renderUser(currentUser);

    if (currentUser) {
        document.getElementById("queue-section").classList.remove("hidden");
        document.getElementById("auth-buttons").innerHTML =
            `<a href="/.auth/logout" class="px-4 py-2 bg-red-600 text-white rounded">Logout</a>`;
    } else {
        document.getElementById("auth-buttons").innerHTML =
            `<a href="/.auth/login/aad" class="px-4 py-2 bg-blue-600 text-white rounded">Login</a>`;
    }

    document.getElementById("btn-create-queue").onclick = createQueueItem;
    document.getElementById("btn-get-queues").onclick = getQueues;
    document.getElementById("btn-next").onclick = popNextUser;
    document.getElementById("btn-status").onclick = getUserStatus;
    document.getElementById("btn-clear-queue").onclick = clearQueue;
}

init();
