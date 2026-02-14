/**
 * Unity MCP Server - Node.js Client Example
 * 
 * This script demonstrates how to connect to and interact with the Unity MCP Server.
 */

const net = require('net');

class UnityMCPClient {
    constructor(host = 'localhost', port = 8765, timeout = 30000) {
        this.host = host;
        this.port = port;
        this.timeout = timeout;
        this.client = null;
        this.requestId = 0;
        this.connected = false;
        this.buffer = '';
    }

    connect() {
        return new Promise((resolve, reject) => {
            this.client = new net.Socket();
            this.client.setTimeout(this.timeout);

            this.client.connect(this.port, this.host, () => {
                this.connected = true;
                console.log(`✓ Connected to Unity MCP Server at ${this.host}:${this.port}`);
                resolve(true);
            });

            this.client.on('error', (err) => {
                console.log(`✗ Connection failed: ${err.message}`);
                reject(err);
            });

            this.client.on('timeout', () => {
                console.log('✗ Connection timeout');
                this.client.destroy();
                reject(new Error('Connection timeout'));
            });
        });
    }

    sendRequest(method, params = {}) {
        return new Promise((resolve, reject) => {
            if (!this.connected) {
                reject(new Error('Not connected to server'));
                return;
            }

            this.requestId++;
            const request = {
                jsonrpc: "2.0",
                id: String(this.requestId),
                method: method,
                params: params
            };

            const responseHandler = (data) => {
                this.buffer += data.toString();
                
                const newlineIndex = this.buffer.indexOf('\n');
                if (newlineIndex >= 0) {
                    const message = this.buffer.substring(0, newlineIndex);
                    this.buffer = this.buffer.substring(newlineIndex + 1);
                    
                    try {
                        const response = JSON.parse(message);
                        
                        if (response.error) {
                            console.log(`✗ Error ${response.error.code}: ${response.error.message}`);
                            if (response.error.data) {
                                console.log(`  Details: ${response.error.data}`);
                            }
                        }
                        
                        resolve(response);
                    } catch (err) {
                        reject(new Error(`Failed to parse response: ${err.message}`));
                    }
                    
                    this.client.removeListener('data', responseHandler);
                }
            };

            this.client.on('data', responseHandler);
            this.client.write(JSON.stringify(request) + '\n');
        });
    }

    async initialize() {
        return this.sendRequest('initialize', {
            protocolVersion: '2025-11-25',
            clientInfo: {
                name: 'Unity MCP Node.js Client',
                version: '1.0.0'
            }
        });
    }

    async listTools() {
        return this.sendRequest('tools/list', {});
    }

    async callTool(toolName, args) {
        return this.sendRequest('tools/call', {
            name: toolName,
            arguments: args
        });
    }

    async ping(message = '') {
        const params = message ? { message } : {};
        return this.sendRequest('ping', params);
    }

    async createScene(name, path = 'Assets/Scenes', setup = 'default') {
        return this.sendRequest('create_scene', {
            name,
            path,
            setup
        });
    }

    async createGameObject(name, type = 'empty', position = null, parent = null) {
        const params = { name, type };
        if (position) params.position = position;
        if (parent) params.parent = parent;
        return this.sendRequest('create_gameobject', params);
    }

    async getSceneInfo(includeHierarchy = true, includeComponents = false) {
        return this.sendRequest('get_scene_info', {
            includeHierarchy,
            includeComponents
        });
    }

    async createScript(name, type = 'monobehaviour', path = 'Assets/Scripts', namespace = null) {
        const params = { name, type, path };
        if (namespace) params.namespace = namespace;
        return this.sendRequest('create_script', params);
    }

    close() {
        if (this.client) {
            this.client.destroy();
            this.connected = false;
            console.log('✓ Connection closed');
        }
    }
}

async function main() {
    console.log('Unity MCP Server - Node.js Client Example');
    console.log('='.repeat(50));
    console.log();

    const client = new UnityMCPClient();

    try {
        // Connect
        await client.connect();
        console.log();

        // Initialize
        console.log('1. Initializing connection...');
        const initResponse = await client.initialize();
        if (initResponse.result) {
            const serverInfo = initResponse.result.serverInfo;
            console.log(`   Connected to: ${serverInfo.name} v${serverInfo.version}`);
        }
        console.log();

        // Ping test
        console.log('2. Testing connectivity...');
        const pingResponse = await client.ping('Hello Unity!');
        if (pingResponse.result) {
            console.log(`   Server responded: ${pingResponse.result.message}`);
        }
        console.log();

        // List tools
        console.log('3. Listing available tools...');
        const toolsResponse = await client.listTools();
        if (toolsResponse.result && toolsResponse.result.tools) {
            const tools = toolsResponse.result.tools;
            console.log(`   Found ${tools.length} tools:`);
            tools.forEach(tool => {
                console.log(`   - ${tool.name}: ${tool.description}`);
            });
        }
        console.log();

        // Get scene info
        console.log('4. Getting current scene info...');
        const sceneInfoResponse = await client.getSceneInfo(false);
        if (sceneInfoResponse.result) {
            const scene = sceneInfoResponse.result;
            console.log(`   Scene: ${scene.name}`);
            console.log(`   Path: ${scene.path}`);
            console.log(`   Objects: ${scene.totalObjectCount || 'N/A'}`);
        }
        console.log();

        // Create a new scene
        console.log('5. Creating new scene...');
        const createSceneResponse = await client.createScene('ExampleScene', 'Assets/Scenes', 'default');
        if (createSceneResponse.result) {
            const result = createSceneResponse.result;
            if (result.success) {
                console.log(`   ✓ ${result.message}`);
            } else {
                console.log(`   ✗ ${result.error || 'Unknown error'}`);
            }
        }
        console.log();

        // Create GameObjects
        console.log('6. Creating GameObjects...');
        
        const cubeResponse = await client.createGameObject('PlayerCube', 'cube', { x: 0, y: 1, z: 0 });
        if (cubeResponse.result && cubeResponse.result.success) {
            console.log('   ✓ Created cube at position (0, 1, 0)');
        }

        const sphereResponse = await client.createGameObject('EnemySphere', 'sphere', { x: 5, y: 1, z: 0 });
        if (sphereResponse.result && sphereResponse.result.success) {
            console.log('   ✓ Created sphere at position (5, 1, 0)');
        }
        console.log();

        // Create a script
        console.log('7. Creating a MonoBehaviour script...');
        const scriptResponse = await client.createScript('PlayerController', 'monobehaviour', 'Assets/Scripts', 'Game.Player');
        if (scriptResponse.result) {
            const result = scriptResponse.result;
            if (result.success) {
                console.log(`   ✓ ${result.message}`);
            } else {
                console.log(`   ✗ ${result.error || 'Unknown error'}`);
            }
        }
        console.log();

        // Get final scene info
        console.log('8. Final scene state...');
        const finalSceneResponse = await client.getSceneInfo(false);
        if (finalSceneResponse.result) {
            const scene = finalSceneResponse.result;
            console.log(`   Scene now has ${scene.totalObjectCount || 'N/A'} total objects`);
        }
        console.log();

        // Close connection
        client.close();

        console.log();
        console.log('='.repeat(50));
        console.log('Example completed successfully!');

    } catch (error) {
        console.error('\nError:', error.message);
        client.close();
        process.exit(1);
    }
}

// Run if executed directly
if (require.main === module) {
    main().catch(err => {
        console.error('Fatal error:', err);
        process.exit(1);
    });
}

module.exports = UnityMCPClient;
