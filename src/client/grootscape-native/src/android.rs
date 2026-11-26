use jni::{
    EnvUnowned,
    errors::ThrowRuntimeExAndDefault,
    objects::{JClass, JString},
};

/// Implements `dev.grootscape.Engine.version()`.
#[unsafe(no_mangle)]
pub extern "system" fn Java_dev_grootscape_Engine_version<'caller>(
    mut env: EnvUnowned<'caller>,
    _class: JClass<'caller>,
) -> JString<'caller> {
    env.with_env(|env| JString::from_str(env, grootscape_engine::VERSION))
        .resolve::<ThrowRuntimeExAndDefault>()
}
