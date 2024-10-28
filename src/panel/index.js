const panel = new Panel('flag-panel');
const socket = new WebSocket('wss://race-control.justinvanderkruit.nl');

socket.addEventListener('message', event => {
    setTimeout(() => {
        handleMessage(event.data)
    }, 22_000);
});

const handleMessage = (message) => {
    const data = JSON.parse(message);
    const flag = data.Flag;

    switch (flag) {
        case 'Clear':
            panel.greenFlag();
            break;
        case 'Yellow':
            panel.yellowFlag();
            break;
        case 'DoubleYellow':
            panel.doubleYellowFlag();
            break;
        case 'Blue':
            panel.blueFlag(data.Driver ?? null);
            break;
        case 'BlackWhite':
            panel.blackWhiteFlag(data.Driver);
            break;
        case 'Red':
            panel.redFlag();
            break;
        case 'SafetyCar':
            panel.safetyCar('SC');
            break;
        case 'Fyc':
            panel.fullCourseYellow();
            break;
        case 'Vsc':
            panel.safetyCar('VSC');
            break;
        case 'Chequered':
            panel.chequeredFlag();
            break;
        case 'Surface':
            panel.slipperySurfaceFlag();
            break;
        default:
            console.warn(`Flag ${flag} not supported`);
    }
}
